namespace Atlas.SearchTracking.Models
{
    public class MatchPredictionCompletedEvent
    {
        public int SearchRequestId { get; set; }
        public DateTime CompletionTimeUtc { get; set; }
        public MatchPredictionCompletionDetails CompletionDetails { get; set; }
    }

    public class MatchPredictionCompletionDetails
    {
        public bool IsSuccessful { get; set; }
        public string? FailureInfoJson { get; set; }
        public int? DonorsPerBatch { get; set; }
        public int TotalNumberOfBatches { get; set; }
    }
}
