using Atlas.Client.Models.Common.Results;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;

namespace Atlas.Functions.Services.MatchCategories
{
    internal interface IPositionalMatchCategoryService
    {
        /// <summary>
        /// Re-orientates positional match categories within <paramref name="matchProbabilityResponse"/> in line with mismatch info held within <paramref name="scoringResult"/>,
        /// only when the donor has both been predicted to be <see cref="PredictiveMatchCategory.Mismatch"/> AND scored as a <see cref="MatchCategory.Mismatch"/>.
        /// </summary>
        MatchProbabilityResponse ReOrientatePositionalMatchCategories(
            MatchProbabilityResponse matchProbabilityResponse,
            ScoringResult scoringResult);
    }

    internal class PositionalMatchCategoryService : IPositionalMatchCategoryService
    {
        private readonly IPositionalMatchCategoryOrientator orientator;

        public PositionalMatchCategoryService(IPositionalMatchCategoryOrientator orientator)
        {
            this.orientator = orientator;
        }

        /// <inheritdoc />
        public MatchProbabilityResponse ReOrientatePositionalMatchCategories(
            MatchProbabilityResponse matchProbabilityResponse,
            ScoringResult scoringResult)
        {
            if (matchProbabilityResponse is not { OverallMatchCategory: PredictiveMatchCategory.Mismatch } ||
                scoringResult is not { MatchCategory: MatchCategory.Mismatch })
            {
                return matchProbabilityResponse;
            }

            var locusScoringResults = scoringResult.ScoringResultsByLocus.ToLociInfo();

            matchProbabilityResponse.MatchProbabilitiesPerLocus.ForEachLocus((locus, locusProbabilities) =>
            {
                if (locusProbabilities == null)
                {
                    return;
                }

                var locusScore = locusScoringResults.GetLocus(locus);
                var matchCategories = orientator.AlignCategoriesToMismatchScore(locusProbabilities.PositionalMatchCategories, locusScore);
                locusProbabilities.PositionalMatchCategories = matchCategories;
            });

            return matchProbabilityResponse;
        }
    }
}