using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.SearchTracking.Data.Models;
using AutoFixture.Dsl;

namespace Atlas.SearchTracking.Data.Test.Builders;

public class MatchingAlgorithmAttemptBuilder
{
    public static IPostprocessComposer<SearchRequestMatchingAlgorithmAttempts> New => FixtureBuilder.For<SearchRequestMatchingAlgorithmAttempts>();

    public static IPostprocessComposer<SearchRequestMatchingAlgorithmAttempts> Default => FixtureBuilder.For<SearchRequestMatchingAlgorithmAttempts>()
        .With(m => m.Id, 1)
        .With(m => m.SearchRequestId, 1)
        .With(m => m.AttemptNumber, 2)
        .With(m => m.InitiationTimeUtc, new DateTime(2021, 1, 1))
        .With(m => m.StartTimeUtc, new DateTime(2021, 1, 1));

    public static IPostprocessComposer<SearchRequestMatchingAlgorithmAttempts> UpdateTimings => Default
        .With(m => m.AlgorithmCore_Matching_StartTimeUtc, new DateTime(2022, 12, 31))
        .With(m => m.AlgorithmCore_Matching_EndTimeUtc, new DateTime(2022, 12, 31))
        .With(m => m.AlgorithmCore_Scoring_StartTimeUtc, new DateTime(2022, 12, 31))
        .With(m => m.AlgorithmCore_Scoring_EndTimeUtc, new DateTime(2022, 12, 31))
        .With(m => m.PersistingResults_StartTimeUtc, new DateTime(2022, 12, 31))
        .With(m => m.PersistingResults_EndTimeUtc, new DateTime(2022, 12, 31));

    public static IPostprocessComposer<SearchRequestMatchingAlgorithmAttempts> Completed => Default
        .With(m => m.CompletionTimeUtc, new DateTime(2022, 1, 1))
        .With(m => m.IsSuccessful, true);
}
