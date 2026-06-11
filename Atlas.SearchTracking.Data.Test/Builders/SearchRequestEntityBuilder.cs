using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Data.Models;
using AutoFixture.Dsl;

namespace Atlas.SearchTracking.Data.Test.Builders;

public static class SearchRequestEntityBuilder
{
    public static IPostprocessComposer<SearchRequest> New => FixtureBuilder.For<SearchRequest>();

    public static IPostprocessComposer<SearchRequest> Default => FixtureBuilder.For<SearchRequest>()
        .With(m => m.Id, 1)
        .With(m => m.SearchIdentifier, new Guid("aaaaaaaa-bbbb-cccc-dddd-000000000000"))
        .With(m => m.IsRepeatSearch, false)
        .With(m => m.OriginalSearchIdentifier, new Guid("11111111-2222-3333-4444-567567567567"))
        .With(m => m.RepeatSearchCutOffDate, new DateTime(2021, 1, 1))
        .With(m => m.RequestJson, "RequestJson")
        .With(m => m.SearchCriteria, "SearchCriteria")
        .With(m => m.DonorType, "DonorType")
        .With(m => m.RequestTimeUtc, new DateTime(2021, 1, 1));

    public static IPostprocessComposer<SearchRequest> NewRecord => FixtureBuilder.For<SearchRequest>()
        .With(m => m.Id, 2)
        .With(m => m.SearchIdentifier, new Guid("eeeeeeee-bbbb-cccc-dddd-000000000000"))
        .With(m => m.IsRepeatSearch, false)
        .With(m => m.OriginalSearchIdentifier, new Guid("11111111-2222-3333-4444-567567567567"))
        .With(m => m.RepeatSearchCutOffDate, new DateTime(2021, 1, 1))
        .With(m => m.RequestJson, "RequestJson")
        .With(m => m.SearchCriteria, "SearchCriteria")
        .With(m => m.DonorType, "DonorType")
        .With(m => m.RequestTimeUtc, new DateTime(2021, 1, 1))
        .With(m => m.AreBetterMatchesIncluded, true)
        .With(m => m.IsMatchPredictionRun, true)
        .With(m => m.DonorRegistryCodes, ["A, B"]);

    public static IPostprocessComposer<SearchRequest> WithMatchingPredictionCompleted => Default
        .With(m => m.MatchPrediction_IsSuccessful, true)
        .With(m => m.MatchPrediction_DonorsPerBatch, 10)
        .With(m => m.MatchPrediction_TotalNumberOfBatches, 1);

    public static IPostprocessComposer<SearchRequest> WithMatchingPredictionNotCompleted => Default
        .With(m => m.MatchPrediction_IsSuccessful, false)
        .With(m => m.MatchPrediction_FailureInfo_Message, "FailureInfoMessage")
        .With(m => m.MatchPrediction_FailureInfo_ExceptionStacktrace, "StackTrace")
        .With(m => m.MatchPrediction_FailureInfo_Type, MatchPredictionFailureType.UnexpectedError);

    public static IPostprocessComposer<SearchRequest> WithMatchingAlgorithmCompleted => Default
        .With(m => m.MatchingAlgorithm_IsSuccessful, true)
        .With(m => m.MatchingAlgorithm_TotalAttemptsNumber, (byte?)3)
        .With(m => m.MatchingAlgorithm_HlaNomenclatureVersion, "3.6.0")
        .With(m => m.MatchingAlgorithm_NumberOfResults, 2000)
        .With(m => m.MatchingAlgorithm_RepeatSearch_AddedResultCount, 50)
        .With(m => m.MatchingAlgorithm_RepeatSearch_RemovedResultCount, 10)
        .With(m => m.MatchingAlgorithm_RepeatSearch_UpdatedResultCount, 5)
        .With(m => m.MatchingAlgorithm_ResultsSent, true)
        .With(m => m.MatchingAlgorithm_ResultsSentTimeUtc, new DateTime(2023, 1, 1));

    public static IPostprocessComposer<SearchRequest> WithMatchingAlgorithmNotCompleted => Default
        .With(m => m.MatchingAlgorithm_IsSuccessful, false)
        .With(m => m.MatchingAlgorithm_ResultsSent, false)
        .With(m => m.MatchingAlgorithm_TotalAttemptsNumber, (byte?)0)
        .With(m => m.MatchingAlgorithm_FailureInfo_Message, "FailureInfoMessage")
        .With(m => m.MatchingAlgorithm_FailureInfo_ExceptionStacktrace, "StackTrace")
        .With(m => m.MatchingAlgorithm_FailureInfo_Type, MatchingAlgorithmFailureType.ValidationError);

    public static IPostprocessComposer<SearchRequest> WithSearchRequestCompleted => Default
        .With(m => m.ResultsSent, true)
        .With(m => m.ResultsSentTimeUtc, new DateTime(2023, 1, 1));

    public static IPostprocessComposer<SearchRequest> WithMatchPredictionResultsSent => Default
        .With(m => m.ResultsSent, true)
        .With(m => m.ResultsSentTimeUtc, new DateTime(2023, 1, 1));

}
