using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Test.TestHelpers.Builders;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Models
{
    [TestFixture]
    public class MatchProbabilityPerLocusResponseTests
    {
        [TestCase(1, 0, 0, PredictiveMatchCategory.Exact, PredictiveMatchCategory.Exact)]
        [TestCase(0, 1, 0, PredictiveMatchCategory.Mismatch, PredictiveMatchCategory.Exact)]
        [TestCase(0, 0, 1, PredictiveMatchCategory.Mismatch, PredictiveMatchCategory.Mismatch)]
        [TestCase(0, 0.5, 0.5, PredictiveMatchCategory.Mismatch, PredictiveMatchCategory.Potential)]
        [TestCase(0.5, 0.5, 0, PredictiveMatchCategory.Potential, PredictiveMatchCategory.Exact)]
        [TestCase(0.2, 0.6, 0.2, PredictiveMatchCategory.Potential, PredictiveMatchCategory.Potential)]
        public void PredictiveMatchCategory_ReturnsExpectedMatchCategories(
            decimal zeroMismatchValue,
            decimal oneMismatchValue,
            decimal twoMismatchValue,
            PredictiveMatchCategory matchCategory1,
            PredictiveMatchCategory matchCategory2)
        {
            var matchProbability = MatchProbabilitiesBuilder.New
                .WithProbabilityValuesSetTo(zeroMismatchValue, oneMismatchValue, twoMismatchValue).Build();

            var matchProbabilityPerLocusResponse = new MatchProbabilityPerLocusResponse(matchProbability);

            var expectedPositionalMatchCategories = 
                new LocusInfo<PredictiveMatchCategory>(matchCategory1, matchCategory2);

            matchProbabilityPerLocusResponse.PositionalMatchCategories.Should().BeEquivalentTo(expectedPositionalMatchCategories);
        }
    }
}
