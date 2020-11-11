using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.Test.Verification.Models;

namespace Atlas.MatchPrediction.Test.Verification.Services.Verification.Compilation
{
    internal interface IProbabilityBinCalculator
    {
        /// <summary>
        /// Note: the final bin will cover probability values from 90-100% inclusive.
        /// </summary>
        IEnumerable<ProbabilityBin> CalculateDecileProbabilityBins(IReadOnlyCollection<ActualVersusExpectedResult> actualVersusExpectedResults);
    }

    internal class ProbabilityBin
    {
        public decimal WeightedMidpoint { get; set; }
        public int TotalPdpCount { get; set; }
        public decimal ActuallyMatchedPercentage { get; set; }
    }

    internal class ProbabilityBinCalculator : IProbabilityBinCalculator
    {
        public IEnumerable<ProbabilityBin> CalculateDecileProbabilityBins(IReadOnlyCollection<ActualVersusExpectedResult> actualVersusExpectedResults)
        {
            if (actualVersusExpectedResults.IsNullOrEmpty())
            {
                throw new ArgumentNullException(nameof(actualVersusExpectedResults));
            }

            return AggregateResultsIntoProbabilityBins(actualVersusExpectedResults);
        }

        private static IEnumerable<ProbabilityBin> AggregateResultsIntoProbabilityBins(IReadOnlyCollection<ActualVersusExpectedResult> actualVersusExpectedResults)
        {
            var upperBounds = Enumerable.Range(1, 10).Select(i => i * 10);

            var bins = new List<ProbabilityBin>();
            upperBounds.Aggregate(
                0,
                (inclusiveLowerBound, exclusiveUpperBound) =>
                {
                    exclusiveUpperBound = exclusiveUpperBound == 100 ? 101 : exclusiveUpperBound;
                    bins.Add(CalculateProbabilityBin(actualVersusExpectedResults, inclusiveLowerBound, exclusiveUpperBound));
                    return exclusiveUpperBound;
                });

            return bins;
        }

        private static ProbabilityBin CalculateProbabilityBin(
            IEnumerable<ActualVersusExpectedResult> actualVersusExpectedResults,
            int inclusiveLowerBound,
            int exclusiveUpperBound)
        {
            var results = actualVersusExpectedResults
                .Where(r => r.Probability >= inclusiveLowerBound && r.Probability < exclusiveUpperBound)
                .ToList();

            if (results.IsNullOrEmpty())
            {
                return new ProbabilityBin
                {
                    WeightedMidpoint = (inclusiveLowerBound + exclusiveUpperBound - 1) / 2m,
                    ActuallyMatchedPercentage = 0m,
                    TotalPdpCount = 0
                };
            }

            var totalPdpCount = results.Sum(r => r.TotalPdpCount);

            return new ProbabilityBin
            {
                WeightedMidpoint = (decimal)results.Sum(r => r.Probability * r.TotalPdpCount) / totalPdpCount,
                ActuallyMatchedPercentage = (decimal)100 * results.Sum(r => r.ActuallyMatchedPdpCount) / totalPdpCount,
                TotalPdpCount = totalPdpCount
            };
        }
    }
}
