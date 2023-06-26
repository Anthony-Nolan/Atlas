using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using LochNessBuilder;
using ResultBuilder = LochNessBuilder.Builder<Atlas.Client.Models.Search.Results.Matching.PerLocus.LocusSearchResult>;

namespace Atlas.Functions.Test.Builders
{
    [Builder]
    internal static class LocusScoreResultBuilder
    {
        public static ResultBuilder New => ResultBuilder.New;

        public static ResultBuilder WithMatchGradesAtBothPositions(this ResultBuilder builder, LocusMatchCategory matchCategory, MatchGrade matchGrade)
        {
            return builder.WithMatchGrades(matchCategory, matchGrade, matchGrade);
        }

        public static ResultBuilder WithMatchGrades(this ResultBuilder builder, LocusMatchCategory matchCategory, MatchGrade matchGrade1, MatchGrade matchGrade2)
        {
            return builder
                .With(x => x.MatchCategory, matchCategory)
                .With(x => x.ScoreDetailsAtPositionOne, new LocusPositionScoreDetails { MatchGrade = matchGrade1 })
                .With(x => x.ScoreDetailsAtPositionTwo, new LocusPositionScoreDetails { MatchGrade = matchGrade2 });
        }
    }
}