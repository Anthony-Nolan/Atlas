using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using ResultBuilder = AutoFixture.Dsl.IPostprocessComposer<Atlas.Client.Models.Common.Results.LocusSearchResult>;

namespace Atlas.Functions.Test.Builders;

internal static class LocusScoreResultBuilder
{
    public static ResultBuilder New => FixtureBuilder.For<Atlas.Client.Models.Common.Results.LocusSearchResult>();

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