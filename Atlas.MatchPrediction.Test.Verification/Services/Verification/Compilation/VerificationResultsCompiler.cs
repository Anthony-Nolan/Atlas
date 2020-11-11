using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.Test.Verification.Models;
using MathNet.Numerics.LinearRegression;

namespace Atlas.MatchPrediction.Test.Verification.Services.Verification.Compilation
{
    internal interface IVerificationResultsCompiler
    {
        Task<VerificationResult> CompileVerificationResults(CompileResultsRequest request);
    }

    internal class VerificationResultsCompiler : IVerificationResultsCompiler
    {
        private readonly IActualVersusExpectedResultsCompiler avEResultsCompiler;
        private readonly IProbabilityBinCalculator probabilityBinCalculator;

        public VerificationResultsCompiler(
            IActualVersusExpectedResultsCompiler avEResultsCompiler,
            IProbabilityBinCalculator probabilityBinCalculator)
        {
            this.avEResultsCompiler = avEResultsCompiler;
            this.probabilityBinCalculator = probabilityBinCalculator;
        }

        public async Task<VerificationResult> CompileVerificationResults(CompileResultsRequest request)
        {
            var actualVersusExpectedResults = (await avEResultsCompiler.CompileResults(request)).ToList();
            var bins = probabilityBinCalculator.CalculateDecileProbabilityBins(actualVersusExpectedResults).ToList();
            var totalPdpCount = bins.Sum(bin => bin.TotalPdpCount);

            return new VerificationResult
            {
                Request = request,
                ActualVersusExpectedResults = actualVersusExpectedResults,
                TotalPdpCount = totalPdpCount,
                WeightedCityBlockDistance = CalculateWeightedCityBlockDistance(bins, totalPdpCount),
                WeightedLinearRegression = PerformWeightedLinearRegression(bins)
            };
        }

        private static decimal CalculateWeightedCityBlockDistance(IEnumerable<ProbabilityBin> bins, int totalPdpCount)
        {
            return bins.Sum(bin => 
                Math.Abs(bin.ActuallyMatchedPercentage - bin.WeightedMidpoint) * bin.TotalPdpCount) / totalPdpCount;
        }

        private static LinearRegression PerformWeightedLinearRegression(IReadOnlyCollection<ProbabilityBin> bins)
        {
            var xValues = bins.Select(bin => Convert.ToDouble(bin.WeightedMidpoint)).Select(x => new[] { 1, x }).ToArray();
            var yValues = bins.Select(bin => Convert.ToDouble(bin.ActuallyMatchedPercentage)).ToArray();
            var weights = bins.Select(bin => Convert.ToDouble(bin.TotalPdpCount)).ToArray();

            var weightedRegression = WeightedRegression.Weighted(xValues, yValues, weights);

            if (weightedRegression.Length != 2)
            {
                throw new Exception($"Weight regression array has {weightedRegression.Length} results instead of the expected, 2.");
            }

            return new LinearRegression
            {
                Intercept = Convert.ToDecimal(weightedRegression[0]),
                Slope = Convert.ToDecimal(weightedRegression[1])
            };
        }
    }
}