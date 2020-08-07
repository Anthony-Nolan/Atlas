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
        [Test]
        public void PredictiveMatchCategory_WhenGuranteedNoMismatches_IsExact()
        {
            var matchProbability = MatchProbabilitiesBuilder.New.WithProbabilityValuesSetTo(1m, 0m, 0m).Build();

            var matchProbabilityPerLocusResponse = new MatchProbabilityPerLocusResponse(matchProbability);

            var expectedPositionalMatchCategories = 
                new LocusInfo<PredictiveMatchCategory>(PredictiveMatchCategory.Exact, PredictiveMatchCategory.Exact);

            matchProbabilityPerLocusResponse.PositionalMatchCategories.Should().BeEquivalentTo(expectedPositionalMatchCategories);
        }

        [Test]
        public void PredictiveMatchCategory_WhenMatchCategoryIsMismatchAndGuranteedSingleMismatch_IsExact()
        {
            var matchProbability = MatchProbabilitiesBuilder.New.WithProbabilityValuesSetTo(0m, 1m, 0m).Build();

            var matchProbabilityPerLocusResponse = new MatchProbabilityPerLocusResponse(matchProbability);

            var expectedPositionalMatchCategories =
                new LocusInfo<PredictiveMatchCategory>(PredictiveMatchCategory.Mismatch, PredictiveMatchCategory.Exact);

            matchProbabilityPerLocusResponse.PositionalMatchCategories.Should().BeEquivalentTo(expectedPositionalMatchCategories);
        }

        [Test]
        public void PredictiveMatchCategory_WhenMatchCategoryIsMismatchAndGuranteedDoubleMismatch_IsMismatch()
        {
            var matchProbability = MatchProbabilitiesBuilder.New.WithProbabilityValuesSetTo(0m, 0m, 1m).Build();

            var matchProbabilityPerLocusResponse = new MatchProbabilityPerLocusResponse(matchProbability);

            var expectedPositionalMatchCategories =
                new LocusInfo<PredictiveMatchCategory>(PredictiveMatchCategory.Mismatch, PredictiveMatchCategory.Mismatch);

            matchProbabilityPerLocusResponse.PositionalMatchCategories.Should().BeEquivalentTo(expectedPositionalMatchCategories);
        }

        [Test]
        public void PredictiveMatchCategory_WhenMatchCategoryIsMismatchAndSingleAndDoubleMismatchNotGuranteed_IsPotential()
        {
            var matchProbability = MatchProbabilitiesBuilder.New.WithProbabilityValuesSetTo(0m, 0.5m, 0.5m).Build();

            var matchProbabilityPerLocusResponse = new MatchProbabilityPerLocusResponse(matchProbability);

            var expectedPositionalMatchCategories =
                new LocusInfo<PredictiveMatchCategory>(PredictiveMatchCategory.Mismatch, PredictiveMatchCategory.Potential);

            matchProbabilityPerLocusResponse.PositionalMatchCategories.Should().BeEquivalentTo(expectedPositionalMatchCategories);
        }

        [Test] public void PredictiveMatchCategory_WhenMatchCategoryIsPotentialAndNotDoubleMismatchGuranteed_IsPotential()
        {
            var matchProbability = MatchProbabilitiesBuilder.New.WithProbabilityValuesSetTo(0.5m, 0.5m, 0m).Build();

            var matchProbabilityPerLocusResponse = new MatchProbabilityPerLocusResponse(matchProbability);

            var expectedPositionalMatchCategories =
                new LocusInfo<PredictiveMatchCategory>(PredictiveMatchCategory.Potential, PredictiveMatchCategory.Exact);

            matchProbabilityPerLocusResponse.PositionalMatchCategories.Should().BeEquivalentTo(expectedPositionalMatchCategories);
        }

        [Test]
        public void PredictiveMatchCategory_WhenMatchCategoryIsPotentialAndNoMismatchGuranteedOrNot_IsExact()
        {
            var matchProbability = MatchProbabilitiesBuilder.New.WithProbabilityValuesSetTo(0.2m, 0.6m, 0.2m).Build();

            var matchProbabilityPerLocusResponse = new MatchProbabilityPerLocusResponse(matchProbability);

            var expectedPositionalMatchCategories =
                new LocusInfo<PredictiveMatchCategory>(PredictiveMatchCategory.Potential, PredictiveMatchCategory.Potential);

            matchProbabilityPerLocusResponse.PositionalMatchCategories.Should().BeEquivalentTo(expectedPositionalMatchCategories);
        }
    }
}
