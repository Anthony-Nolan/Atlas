﻿using Atlas.SearchTracking.Common.Enums;

namespace Atlas.SearchTracking.Common.Models
{
    public class MatchingAlgorithmCompletedEvent : ISearchTrackingMatchingAttemptEvent
    {
        public Guid SearchIdentifier { get; set; }
        public Guid? OriginalSearchIdentifier { get; set; }
        public byte AttemptNumber { get; set; }
        public DateTime CompletionTimeUtc { get; set; }
        public MatchingAlgorithmCompletionDetails CompletionDetails { get; set; } = null!;
        public string HlaNomenclatureVersion { get; set; } = null!;
        public bool ResultsSent { get; set; }
        public DateTime? ResultsSentTimeUtc { get; set; }
    }

    public class MatchingAlgorithmCompletionDetails
    {
        public bool IsSuccessful { get; set; }
        public MatchingAlgorithmFailureInfo? FailureInfo { get; set; }
        public byte TotalAttemptsNumber { get; set; }
        public int? NumberOfResults { get; set; }
        public MatchingAlgorithmRepeatSearchResultsDetails? RepeatSearchResultsDetails { get; set; }
    }

    public class MatchingAlgorithmFailureInfo
    {
        public string? Message { get; set; }
        public string? ExceptionStacktrace { get; set; }
        public MatchingAlgorithmFailureType? Type { get; set; }
    }

    public class MatchingAlgorithmRepeatSearchResultsDetails
    {
        public int? AddedResultCount { get; set; }
        public int? RemovedResultCount { get; set; }
        public int? UpdatedResultCount { get; set; }
    }
}
