using System.Threading.Tasks;
using Atlas.MatchPrediction.Test.Verification.Services.Verification.Compilation;
using FluentAssertions;
using LochNessBuilder;
using MathNet.Numerics.LinearRegression;
using NSubstitute;
using NUnit.Framework;
using BinBuilder = LochNessBuilder.Builder<Atlas.MatchPrediction.Test.Verification.Services.Verification.Compilation.ProbabilityBin>;

namespace Atlas.MatchPrediction.Test.Verification.Test.UnitTests
{
    [TestFixture]
    public class VerificationResultsCompilerTests
    {
        private IActualVersusExpectedResultsCompiler avEResultsCompiler;
        private IProbabilityBinCalculator probabilityBinCalculator;

        private IVerificationResultsCompiler compiler;

        [SetUp]
        public void SetUp()
        {
            avEResultsCompiler = Substitute.For<IActualVersusExpectedResultsCompiler>();
            probabilityBinCalculator = Substitute.For<IProbabilityBinCalculator>();

            compiler = new VerificationResultsCompiler(avEResultsCompiler, probabilityBinCalculator);
        }

        [Test]
        public async Task CompileVerificationResults_SumsPdpCountsAcrossAllBins()
        {
            const int bin1Count = 50;
            const int bin2Count = 70;

            var bins = BinBuilder.New
                .With(x => x.WeightedMidpoint, new[] { 5m, 95m })
                .With(x => x.TotalPdpCount, new[] { bin1Count, bin2Count })
                .Build(2);
            probabilityBinCalculator.CalculateDecileProbabilityBins(default).ReturnsForAnyArgs(bins);

            var result = await compiler.CompileVerificationResults(default);

            result.TotalPdpCount.Should().Be(bin1Count + bin2Count);
        }

        [Test]
        public async Task CompileVerificationResults_CalculatesWeightedCityBlockDistance()
        {
            const decimal midpoint1 = 5m;
            const decimal distance1 = 1m;
            const int pdpCount1 = 100;
            var bin1 = BinBuilder.New
                .With(x => x.WeightedMidpoint, midpoint1)
                .With(x => x.ActuallyMatchedPercentage, midpoint1 + distance1)
                .With(x => x.TotalPdpCount, pdpCount1)
                .Build();

            const decimal midpoint2 = 95m;
            const decimal distance2 = 5m;
            const int pdpCount2 = 300;
            var bin2 = BinBuilder.New
                .With(x => x.WeightedMidpoint, midpoint2)
                .With(x => x.ActuallyMatchedPercentage, midpoint2 + distance2)
                .With(x => x.TotalPdpCount, pdpCount2)
                .Build();

            const decimal expected = 4m;

            probabilityBinCalculator.CalculateDecileProbabilityBins(default).ReturnsForAnyArgs(new[] { bin1, bin2 });

            var result = await compiler.CompileVerificationResults(default);

            result.WeightedCityBlockDistance.Should().Be(expected);
        }

        [Test]
        public async Task CompileVerificationResults_PerformsWeightedLinearRegression()
        {
            var midpoints = new[] { 15m, 85m };
            var matchedPercentages = new[] { 12.5m, 87.5m };
            var pdpCounts = new[] { 50, 500 };
            var bins = BinBuilder.New
                .With(x => x.WeightedMidpoint, midpoints)
                .With(x => x.ActuallyMatchedPercentage, matchedPercentages)
                .With(x => x.TotalPdpCount, pdpCounts)
                .Build(2);

            const decimal expectedSlope = 1.07m;
            const decimal expectedIntercept = -3.57m;

            probabilityBinCalculator.CalculateDecileProbabilityBins(default).ReturnsForAnyArgs(bins);

            var result = await compiler.CompileVerificationResults(default);

            result.WeightedLinearRegression.Slope.Should().BeApproximately(expectedSlope, 0.01m);
            result.WeightedLinearRegression.Intercept.Should().BeApproximately(expectedIntercept, 0.01m);
        }
    }
}