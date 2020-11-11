using System.Collections.Generic;
using System.Linq;
using Atlas.MatchPrediction.Test.Verification.Models;
using Atlas.MatchPrediction.Test.Verification.Services.Verification.Compilation;
using Atlas.MatchPrediction.Test.Verification.Test.TestHelpers;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Verification.Test.UnitTests
{
    [TestFixture]
    public class ProbabilityBinCalculatorTests
    {
        private IProbabilityBinCalculator calculator;

        [SetUp]
        public void SetUp()
        {
            calculator = new ProbabilityBinCalculator();
        }

        [Test]
        public void CalculateDecileProbabilityBins_ReturnsTenBins()
        {
            var bins = calculator.CalculateDecileProbabilityBins(BuildResults());

            bins.Count().Should().Be(10);
        }

        [Test]
        public void CalculateDecileProbabilityBins_ForFirstNineBins_CountsPdpsOf10ProbabilitiesPerBin([Range(0, 8)] int binIndex)
        {
            const int countPerProbability = 3;
            const int expectedCountPerBin = countPerProbability * 10;

            var bins = calculator.CalculateDecileProbabilityBins(BuildResults(countPerProbability)).ToList();
            var bin = bins[binIndex];

            bin.TotalPdpCount.Should().Be(expectedCountPerBin);
        }

        [Test]
        public void CalculateDecileProbabilityBins_ForLastBin_CountsPdpsOf11Probabilities()
        {
            const int countPerProbability = 3;
            const int expectedCountPerBin = countPerProbability * 11;

            var bins = calculator.CalculateDecileProbabilityBins(BuildResults(countPerProbability)).ToList();
            var lastBin = bins.Last();

            lastBin.TotalPdpCount.Should().Be(expectedCountPerBin);
        }

        [Test]
        public void CalculateDecileProbabilityBins_CalculatesActuallyMatchedPercentage()
        {
            const decimal expectedActuallyMatchedPercentage = 100m;

            var bins = calculator.CalculateDecileProbabilityBins(BuildResults()).ToList();

            bins.Select(b => b.ActuallyMatchedPercentage).Should().AllBeEquivalentTo(expectedActuallyMatchedPercentage);
        }

        [Test]
        public void CalculateDecileProbabilityBins_ForFirstNineBins_CalculatesMidpoint([Range(0, 8)] int binIndex)
        {
            // when every probability has the same pdp count, then weighted and non-weighted midpoints will be equal
            var expectedMidpoint = 10 * binIndex + 4.5m;

            var bins = calculator.CalculateDecileProbabilityBins(BuildResults(5)).ToList();

            bins[binIndex].WeightedMidpoint.Should().Be(expectedMidpoint);
        }

        [Test]
        public void CalculateDecileProbabilityBins_ForLastBin_CalculatesMidpoint()
        {
            // when every probability has the same pdp count, then weighted and non-weighted midpoints will be equal
            const int expectedMidpoint = 95;

            var bins = calculator.CalculateDecileProbabilityBins(BuildResults(5)).ToList();
            var lastBin = bins.Last();

            lastBin.WeightedMidpoint.Should().Be(expectedMidpoint);
        }

        [Test]
        public void CalculateDecileProbabilityBins_CalculatesWeightedMidpoint()
        {
            var resultsForFirstBin = new[]
            {
                ActualVersusExpectedResultBuilder.New
                    .WithProbabilityAndCounts(2, 100)
                    .Build(),
                ActualVersusExpectedResultBuilder.New
                    .WithProbabilityAndCounts(4, 300)
                    .Build()
            };
            const decimal expectedWeightedMidpoint = 3.5m;

            var bins = calculator.CalculateDecileProbabilityBins(resultsForFirstBin).ToList();

            bins.First().WeightedMidpoint.Should().Be(expectedWeightedMidpoint);
        }

        [Test]
        public void CalculateDecileProbabilityBins_ForFirstNineBins_WhenNoProbabilitiesInBin_ReturnsDefaultValues([Range(0,8)] int binIndex)
        {
            var lowerBound = 10 * binIndex;
            var upperBound = lowerBound + 9;
            var expectedMidpoint = lowerBound + 4.5m;
            
            var allResults = BuildResults(1).ToList();
            allResults.RemoveAll(r => r.Probability >= lowerBound && r.Probability <= upperBound);

            var bins = calculator.CalculateDecileProbabilityBins(allResults).ToList();
            var bin = bins[binIndex];

            bin.WeightedMidpoint.Should().Be(expectedMidpoint);
            bin.ActuallyMatchedPercentage.Should().Be(0);
            bin.TotalPdpCount.Should().Be(0);
        }

        [Test]
        public void CalculateDecileProbabilityBins_ForLastBin_WhenNoProbabilitiesInBin_ReturnsDefaultValues()
        {
            const int lowerBound = 90;
            const int upperBound = 100;
            const decimal expectedMidpoint = 95m;
            
            var allResults = BuildResults(1).ToList();
            allResults.RemoveAll(r => r.Probability >= lowerBound && r.Probability <= upperBound);

            var bins = calculator.CalculateDecileProbabilityBins(allResults).ToList();
            var bin = bins.Last();

            bin.WeightedMidpoint.Should().Be(expectedMidpoint);
            bin.ActuallyMatchedPercentage.Should().Be(0);
            bin.TotalPdpCount.Should().Be(0);
        }

        private static IReadOnlyCollection<ActualVersusExpectedResult> BuildResults(int pdpCount = 1)
        {
            return Enumerable.Range(0, 101)
                .Select(p => ActualVersusExpectedResultBuilder.New.WithProbabilityAndCounts(p, pdpCount).Build())
                .ToList();
        }
    }
}