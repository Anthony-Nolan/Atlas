namespace Atlas.SearchTracking.Common.Models
{
    public class MatchingAlgorithmCompletedEvent
    {
        public Guid SearchRequestId { get; set; }
        public byte AttemptNumber { get; set; }
        public DateTime CompletionTimeUtc { get; set; }
        public MatchingAlgorithmCompletionDetails? CompletionDetails { get; set; }
        public string HlaNomenclatureVersion { get; set; }
        public bool ResultsSent { get; set; }
        public DateTime? ResultsSentTimeUtc { get; set; }
    }

    public class MatchingAlgorithmCompletionDetails
    {
        public bool IsSuccessful { get; set; }
        public string? FailureInfoJson { get; set; }
        public byte TotalAttemptsNumber { get; set; }
        public int? NumberOfResults { get; set; }
        public int? NumberOfMatching { get; set; }
        public MatchingAlgorithmRepeatSearchResultsDetails? RepeatSearchResultsDetails { get; set; }
    }

    public class MatchingAlgorithmRepeatSearchResultsDetails
    {
        public int? AddedResultCount { get; set; }
        public int? RemovedResultCount { get; set; }
        public int? UpdatedResultCount { get; set; }
    }
}
