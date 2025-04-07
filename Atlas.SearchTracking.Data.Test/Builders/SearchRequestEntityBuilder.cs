using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Data.Models;
using LochNessBuilder;

namespace Atlas.SearchTracking.Data.Test.Builders
{
    public static class SearchRequestEntityBuilder
    {
        public static Builder<SearchRequest> New => Builder<SearchRequest>.New;

        public static Builder<SearchRequest> Default => Builder<SearchRequest>.New
            .With(m => m.Id, 1)
            .With(m => m.SearchIdentifier, new Guid("aaaaaaaa-bbbb-cccc-dddd-000000000000"))
            .With(m => m.IsRepeatSearch, false)
            .With(m => m.OriginalSearchIdentifier, new Guid("11111111-2222-3333-4444-567567567567"))
            .With(m => m.RepeatSearchCutOffDate, new DateTime(2021, 1, 1))
            .With(m => m.RequestJson, "RequestJson")
            .With(m => m.SearchCriteria, "SearchCriteria")
            .With(m => m.DonorType, "DonorType")
            .With(m => m.RequestTimeUtc, new DateTime(2021, 1, 1));

        public static Builder<SearchRequest> NewRecord => Builder<SearchRequest>.New
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
            .WithSharedRef(m => m.DonorRegistryCodes, ["A, B"]);

        public static Builder<SearchRequest> WithMatchingPredictionCompleted => Default
            .With(m => m.MatchPrediction_IsSuccessful, true)
            .With(m => m.MatchPrediction_DonorsPerBatch, 10)
            .With(m => m.MatchPrediction_TotalNumberOfBatches, 1);

        public static Builder<SearchRequest> WithMatchingPredictionNotCompleted => Default
            .With(m => m.MatchPrediction_IsSuccessful, false)
            .With(m => m.MatchPrediction_FailureInfo_Message, "FailureInfoMessage")
            .With(m => m.MatchPrediction_FailureInfo_ExceptionStacktrace, "StackTrace")
            .With(m => m.MatchPrediction_FailureInfo_Type, MatchPredictionFailureType.UnexpectedError);

        public static Builder<SearchRequest> WithMatchingAlgorithmCompleted => Default
            .With(m => m.MatchingAlgorithm_IsSuccessful, true)
            .With(m => m.MatchingAlgorithm_TotalAttemptsNumber, 3)
            .With(m => m.MatchingAlgorithm_HlaNomenclatureVersion, "3.6.0")
            .With(m => m.MatchingAlgorithm_NumberOfResults, 2000)
            .With(m => m.MatchingAlgorithm_RepeatSearch_AddedResultCount, 50)
            .With(m => m.MatchingAlgorithm_RepeatSearch_RemovedResultCount, 10)
            .With(m => m.MatchingAlgorithm_RepeatSearch_UpdatedResultCount, 5)
            .With(m => m.MatchingAlgorithm_ResultsSent, true)
            .With(m => m.MatchingAlgorithm_ResultsSentTimeUtc, new DateTime(2023, 1, 1));

        public static Builder<SearchRequest> WithMatchingAlgorithmNotCompleted => Default
            .With(m => m.MatchingAlgorithm_IsSuccessful, false)
            .With(m => m.MatchingAlgorithm_ResultsSent, false)
            .With(m => m.MatchingAlgorithm_TotalAttemptsNumber, 0)
            .With(m => m.MatchingAlgorithm_FailureInfo_Message, "FailureInfoMessage")
            .With(m => m.MatchingAlgorithm_FailureInfo_ExceptionStacktrace, "StackTrace")
            .With(m => m.MatchingAlgorithm_FailureInfo_Type, MatchingAlgorithmFailureType.ValidationError);

        public static Builder<SearchRequest> WithSearchRequestCompleted => Default
            .With(m => m.ResultsSent, true)
            .With(m => m.ResultsSentTimeUtc, new DateTime(2023, 1, 1));
    }
}