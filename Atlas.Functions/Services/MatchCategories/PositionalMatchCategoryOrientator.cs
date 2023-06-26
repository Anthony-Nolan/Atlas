using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using LocusMatchCategories = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.LocusInfo<Atlas.Client.Models.Search.Results.MatchPrediction.PredictiveMatchCategory?>;

namespace Atlas.Functions.Services.MatchCategories
{
    public interface IPositionalMatchCategoryOrientator
    {
        LocusMatchCategories AlignCategoriesToMismatchScore(LocusMatchCategories matchCategories, LocusSearchResult locusScoreResult);
    }

    internal class PositionalMatchCategoryOrientator : IPositionalMatchCategoryOrientator
    {
        /// <summary>
        /// Re-orientates <paramref name="matchCategories"/> in line with mismatch info within <paramref name="locusScoreResult"/>,
        /// but only when one category is <see cref="PredictiveMatchCategory.Mismatch"/> and the other is not.
        /// </summary>
        public LocusMatchCategories AlignCategoriesToMismatchScore(LocusMatchCategories matchCategories, LocusSearchResult locusScoreResult)
        {
            if (matchCategories is null || matchCategories.Position1And2Null() ||
                locusScoreResult is not { MatchCategory: LocusMatchCategory.Mismatch })
            {
                return matchCategories;
            }

            var predictiveIsMismatch = matchCategories.Map(category => category == PredictiveMatchCategory.Mismatch);

            // if both positions are predicted to be mismatched, or both matched
            if (predictiveIsMismatch.Position1 == predictiveIsMismatch.Position2)
            {
                return matchCategories;
            }

            var scoreIsMismatch = new LocusInfo<bool>(
                locusScoreResult.ScoreDetailsAtPositionOne?.MatchGrade == MatchGrade.Mismatch,
                locusScoreResult.ScoreDetailsAtPositionTwo?.MatchGrade == MatchGrade.Mismatch);

            // if both positions were scored as mismatched
            if (scoreIsMismatch.Position1 == scoreIsMismatch.Position2)
            {
                return matchCategories;
            }

            return scoreIsMismatch.Position1 == predictiveIsMismatch.Position1
                ? matchCategories
                : matchCategories.Swap();
        }
    }
}