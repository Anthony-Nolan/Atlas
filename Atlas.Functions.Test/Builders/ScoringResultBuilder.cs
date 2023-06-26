using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using LochNessBuilder;

namespace Atlas.Functions.Test.Builders
{
    [Builder]
    internal static class ScoringResultBuilder
    {
        public static Builder<ScoringResult> New = Builder<ScoringResult>.New;

        public static Builder<ScoringResult> MatchedAtEveryLocus(this Builder<ScoringResult> builder)
        {
            var locusScore = LocusScoreResultBuilder.New.WithMatchGradesAtBothPositions(LocusMatchCategory.Match, MatchGrade.PGroup);

            return builder
                .With(x => x.MatchCategory, MatchCategory.Exact)
                .With(x => x.ScoringResultsByLocus, new LociInfo<LocusSearchResult>(locusScore).ToLociInfoTransfer());
        }

        public static Builder<ScoringResult> MismatchedAtEveryLocus(this Builder<ScoringResult> builder)
        {
            var locusScore = LocusScoreResultBuilder.New.WithMatchGradesAtBothPositions(LocusMatchCategory.Mismatch, MatchGrade.Mismatch);

            return builder
                .With(x => x.MatchCategory, MatchCategory.Mismatch)
                .With(x => x.ScoringResultsByLocus, new LociInfo<LocusSearchResult>(locusScore).ToLociInfoTransfer());
        }
    }
}