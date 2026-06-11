using Atlas.Client.Models.Common.Results;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using AutoFixture.Dsl;

namespace Atlas.Functions.Test.Builders;

internal static class ScoringResultBuilder
{
    public static IPostprocessComposer<ScoringResult> New = FixtureBuilder.For<ScoringResult>();

    public static IPostprocessComposer<ScoringResult> MatchedAtEveryLocus(this IPostprocessComposer<ScoringResult> builder)
    {
        var locusScore = LocusScoreResultBuilder.New.WithMatchGradesAtBothPositions(LocusMatchCategory.Match, MatchGrade.PGroup).Build();

        return builder
            .With(x => x.MatchCategory, MatchCategory.Exact)
            .With(x => x.ScoringResultsByLocus, new LociInfo<LocusSearchResult>(locusScore).ToLociInfoTransfer());
    }

    public static IPostprocessComposer<ScoringResult> MismatchedAtEveryLocus(this IPostprocessComposer<ScoringResult> builder)
    {
        var locusScore = LocusScoreResultBuilder.New.WithMatchGradesAtBothPositions(LocusMatchCategory.Mismatch, MatchGrade.Mismatch).Build();

        return builder
            .With(x => x.MatchCategory, MatchCategory.Mismatch)
            .With(x => x.ScoringResultsByLocus, new LociInfo<LocusSearchResult>(locusScore).ToLociInfoTransfer());
    }
}