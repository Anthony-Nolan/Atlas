using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Test.TestHelpers.Builders;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Models
{
    [TestFixture]
    public class MatchProbabilityPerLocusResponseTests
    {
        /// <summary>
        /// Test cases include examples where the probability is exactly 1m or 0m,
        /// and values that are not exactly 1m or 0m, but would round to 100% and 0%, respectively.
        /// </summary>
        [TestCase(1, 0, 0, PredictiveMatchCategory.Exact, PredictiveMatchCategory.Exact)]
        [TestCase(0.995, 0.0025, 0.0025, PredictiveMatchCategory.Exact, PredictiveMatchCategory.Exact)]

        [TestCase(0, 0, 1, PredictiveMatchCategory.Mismatch, PredictiveMatchCategory.Mismatch)]
        [TestCase(0.0025, 0.0025, 0.995, PredictiveMatchCategory.Mismatch, PredictiveMatchCategory.Mismatch)]

        [TestCase(0, 1, 0, PredictiveMatchCategory.Mismatch, PredictiveMatchCategory.Exact)]
        [TestCase(0.005, 0.99, 0.005, PredictiveMatchCategory.Mismatch, PredictiveMatchCategory.Exact)]
        [TestCase(0, 0.5, 0.5, PredictiveMatchCategory.Mismatch, PredictiveMatchCategory.Potential)]
        [TestCase(0.005, 0.495, 0.5, PredictiveMatchCategory.Mismatch, PredictiveMatchCategory.Potential)]

        [TestCase(0.5, 0.5, 0, PredictiveMatchCategory.Potential, PredictiveMatchCategory.Exact)]
        [TestCase(0.5, 0.495, 0.005, PredictiveMatchCategory.Potential, PredictiveMatchCategory.Exact)]
        [TestCase(0.2, 0.6, 0.2, PredictiveMatchCategory.Potential, PredictiveMatchCategory.Potential)]
        public void PredictiveMatchCategory_ReturnsExpectedMatchCategories(
            decimal zeroMismatchValue,
            decimal oneMismatchValue,
            decimal twoMismatchValue,
            PredictiveMatchCategory? matchCategory1,
            PredictiveMatchCategory? matchCategory2)
        {
            var matchProbability = MatchProbabilitiesBuilder.New
                .WithProbabilityValuesSetTo(zeroMismatchValue, oneMismatchValue, twoMismatchValue).Build();

            var matchProbabilityPerLocusResponse = new MatchProbabilityPerLocusResponse(matchProbability);

            var expectedPositionalMatchCategories = new LocusInfo<PredictiveMatchCategory?>(matchCategory1, matchCategory2);

            matchProbabilityPerLocusResponse.PositionalMatchCategories.Should().BeEquivalentTo(expectedPositionalMatchCategories);
        }
    }
}
