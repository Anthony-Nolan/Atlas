using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.SearchTracking.Data.Models;
using AutoFixture.Dsl;

namespace Atlas.SearchTracking.Data.Test.Builders;

public class MatchPredictionEntityBuilder
{
    public static IPostprocessComposer<SearchRequestMatchPrediction> New => FixtureBuilder.For<SearchRequestMatchPrediction>();

    public static IPostprocessComposer<SearchRequestMatchPrediction> Default => FixtureBuilder.For<SearchRequestMatchPrediction>()
        .With(m => m.Id, 1)
        .With(m => m.SearchRequestId, 1)
        .With(m => m.InitiationTimeUtc, new DateTime(2021, 1, 1))
        .With(m => m.StartTimeUtc, new DateTime(2021, 1, 1));

    public static IPostprocessComposer<SearchRequestMatchPrediction> UpdateTimings => Default
        .With(m => m.PrepareBatches_StartTimeUtc, new DateTime(2022, 12, 31))
        .With(m => m.PrepareBatches_EndTimeUtc, new DateTime(2022, 12, 31))
        .With(m => m.AlgorithmCore_RunningBatches_StartTimeUtc, new DateTime(2022, 12, 31))
        .With(m => m.AlgorithmCore_RunningBatches_EndTimeUtc, new DateTime(2022, 12, 31))
        .With(m => m.PersistingResults_StartTimeUtc, new DateTime(2022, 12, 31))
        .With(m => m.PersistingResults_EndTimeUtc, new DateTime(2022, 12, 31));

    public static IPostprocessComposer<SearchRequestMatchPrediction> Completed => Default
        .With(m => m.CompletionTimeUtc, new DateTime(2022, 1, 1))
        .With(m => m.IsSuccessful, true);
}
