using Atlas.Client.Models.Common.Results;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.Functions.Services.MatchCategories;
using Atlas.Functions.Test.Builders;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using LocusMatchCategories = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.LocusInfo<Atlas.Client.Models.Search.Results.MatchPrediction.PredictiveMatchCategory?>;

namespace Atlas.Functions.Test.Services
{
    [TestFixture]
    internal class PositionalMatchCategoryServiceTests
    {
        private static readonly HashSet<Locus> AllowedLoci = new() { Locus.A, Locus.B, Locus.Drb1 };

        private IPositionalMatchCategoryOrientator orientator;
        private IPositionalMatchCategoryService categoryService;

        [SetUp]
        public void SetUp()
        {
            orientator = Substitute.For<IPositionalMatchCategoryOrientator>();
            categoryService = new PositionalMatchCategoryService(orientator);
        }

        [Test]
        public void ReOrientatePositionalMatchCategories_NoMatchProbabilities_ReturnsNull()
        {
            var scoringResult = ScoringResultBuilder.New.Build();

            var response = categoryService.ReOrientatePositionalMatchCategories(null, scoringResult);

            orientator.DidNotReceiveWithAnyArgs().AlignCategoriesToMismatchScore(Arg.Any<LocusMatchCategories>(), Arg.Any<LocusSearchResult>());
            response.Should().BeNull();
        }

        [Test]
        public void ReOrientatePositionalMatchCategories_NoMismatchPredicted_ReturnsOriginalResponse()
        {
            var matchProbabilities = new MatchProbabilityResponse(new Probability(1m), AllowedLoci);
            var scoringResult = ScoringResultBuilder.New.Build();

            var response = categoryService.ReOrientatePositionalMatchCategories(matchProbabilities, scoringResult);

            orientator.DidNotReceiveWithAnyArgs().AlignCategoriesToMismatchScore(Arg.Any<LocusMatchCategories>(), Arg.Any<LocusSearchResult>());
            CategoryAtEveryPositionShouldBe(PredictiveMatchCategory.Exact, response);
        }

        [Test]
        public void ReOrientatePositionalMatchCategories_NoScoringResult_ReturnsOriginalResponse()
        {
            var matchProbabilities = new MatchProbabilityResponse(new Probability(1m), AllowedLoci);

            var response = categoryService.ReOrientatePositionalMatchCategories(matchProbabilities, null);

            orientator.DidNotReceiveWithAnyArgs().AlignCategoriesToMismatchScore(Arg.Any<LocusMatchCategories>(), Arg.Any<LocusSearchResult>());
            CategoryAtEveryPositionShouldBe(PredictiveMatchCategory.Exact, response);
        }

        [Test]
        public void ReOrientatePositionalMatchCategories_NoMismatchScore_ReturnsOriginalResponse()
        {
            var matchProbabilities = new MatchProbabilityResponse(new Probability(1m), AllowedLoci);

            var response = categoryService.ReOrientatePositionalMatchCategories(matchProbabilities, ScoringResultBuilder.New.MatchedAtEveryLocus());

            orientator.DidNotReceiveWithAnyArgs().AlignCategoriesToMismatchScore(Arg.Any<LocusMatchCategories>(), Arg.Any<LocusSearchResult>());
            CategoryAtEveryPositionShouldBe(PredictiveMatchCategory.Exact, response);
        }

        [Test]
        public void ReOrientatePositionalMatchCategories_AlignsCategoriesForEveryAllowedLocus()
        {
            var matchProbabilities = new MatchProbabilityResponse(new Probability(0m), AllowedLoci);

            categoryService.ReOrientatePositionalMatchCategories(matchProbabilities, ScoringResultBuilder.New.MismatchedAtEveryLocus());

            orientator.ReceivedWithAnyArgs(AllowedLoci.Count).AlignCategoriesToMismatchScore(Arg.Any<LocusMatchCategories>(), Arg.Any<LocusSearchResult>());
        }

        private static void CategoryAtEveryPositionShouldBe(PredictiveMatchCategory expectedCategory, MatchProbabilityResponse response)
        {
            response.MatchProbabilitiesPerLocus.ForEachLocus((locus, locusResponse) =>
            {
                if (!AllowedLoci.Contains(locus))
                {
                    return;
                }

                locusResponse.PositionalMatchCategories.EachPosition((_, category) => category.Should().Be(expectedCategory));
            });
        }
    }
}