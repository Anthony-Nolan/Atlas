using Atlas.SearchTracking.Common.Enums;
using System;

namespace Atlas.Debug.Client.Models.SearchTracking
{
    public class SearchTrackingMatchingAlgorithmInfo
    {
        public bool? IsSuccessful { get; set; }

        public string? FailureInfo_Message { get; set; }

        public string? FailureInfo_ExceptionStacktrace { get; set; }

        public MatchingAlgorithmFailureType? FailureInfo_Type { get; set; }

        public byte? TotalAttemptsNumber { get; set; }

        public int? NumberOfResults { get; set; }

        public int? RepeatSearch_AddedResultCount { get; set; }

        public int? RepeatSearch_RemovedResultCount { get; set; }

        public int? RepeatSearch_UpdatedResultCount { get; set; }

        public string? HlaNomenclatureVersion { get; set; }

        public bool? ResultsSent { get; set; }

        public DateTime? ResultsSentTimeUtc { get; set; }
    }
}
