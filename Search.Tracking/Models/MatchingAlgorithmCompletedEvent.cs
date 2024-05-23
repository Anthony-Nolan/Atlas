namespace Atlas.SearchTracking.Models
{
    public class MatchingAlgorithmCompletedEvent
    {
        public int SearchRequestId { get; set; }
        public byte AttemptNumber { get; set; }
        public DateTime CompletionTimeUtc { get; set; }
        public MatchingAlgorithmCompletionDetails CompletionDetails { get; set; }
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
        public int? RepeatSearch_AddedResultCount { get; set; }
        public int? RepeatSearch_RemovedResultCount { get; set; }
        public int? RepeatSearch_UpdatedResultCount { get; set; }
    }
}
