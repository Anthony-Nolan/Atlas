using System.Collections.Generic;
using System;
using Atlas.SearchTracking.Common.Enums;

namespace Atlas.Debug.Client.Models.SearchTracking
{
    public class SearchRequest
    {
        public int Id { get; set; }

        public Guid SearchIdentifier { get; set; }

        public bool IsRepeatSearch { get; set; }

        public Guid? OriginalSearchIdentifier { get; set; }

        public DateTime? RepeatSearchCutOffDate { get; set; }

        public string RequestJson { get; set; }

        public string SearchCriteria { get; set; }

        public string DonorType { get; set; }

        public DateTime RequestTimeUtc { get; set; }

        public bool? MatchingAlgorithm_IsSuccessful { get; set; }

        public string? MatchingAlgorithm_FailureInfo_Message { get; set; }

        public string? MatchingAlgorithm_FailureInfo_ExceptionStacktrace { get; set; }

        public MatchingAlgorithmFailureType? MatchingAlgorithm_FailureInfo_Type { get; set; }

        public byte? MatchingAlgorithm_TotalAttemptsNumber { get; set; }

        public int? MatchingAlgorithm_NumberOfResults { get; set; }

        public int? MatchingAlgorithm_RepeatSearch_AddedResultCount { get; set; }

        public int? MatchingAlgorithm_RepeatSearch_RemovedResultCount { get; set; }

        public int? MatchingAlgorithm_RepeatSearch_UpdatedResultCount { get; set; }

        public string? MatchingAlgorithm_HlaNomenclatureVersion { get; set; }

        public bool? MatchingAlgorithm_ResultsSent { get; set; }

        public DateTime? MatchingAlgorithm_ResultsSentTimeUtc { get; set; }

        public bool? MatchPrediction_IsSuccessful { get; set; }

        public string? MatchPrediction_FailureInfo_Message { get; set; }

        public string? MatchPrediction_FailureInfo_ExceptionStacktrace { get; set; }

        public MatchPredictionFailureType? MatchPrediction_FailureInfo_Type { get; set; }

        public int? MatchPrediction_DonorsPerBatch { get; set; }

        public int? MatchPrediction_TotalNumberOfBatches { get; set; }

        public bool? ResultsSent { get; set; }

        public DateTime? ResultsSentTimeUtc { get; set; }

        public SearchRequestMatchPrediction? SearchRequestMatchPrediction { get; set; }

        public ICollection<SearchRequestMatchingAlgorithmAttempts>? SearchRequestMatchingAlgorithmAttempts { get; set; }

        public bool IsMatchPredictionRun { get; set; }

        public bool AreBetterMatchesIncluded { get; set; }

        public ICollection<string>? DonorRegistryCodes { get; set; }
    }
}