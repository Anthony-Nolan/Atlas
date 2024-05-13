namespace Atlas.SearchTracking.Models
{
    public class MatchPredictionCompletedEvent
    {
        public int SearchRequestId { get; set; }
        public DateTime CompletionTimeUtc { get; set; }
        public bool MatchPrediction_IsSuccessful { get; set; }
        public MatchPredictionCompletionDetails MatchPrediction_FailureInfo_Json { get; set; }
        public int MatchPrediction_DonorsPerBatch { get; set; }
        public int MatchPrediction_TotalNumberOfBatches { get; set; }
    }

    public class MatchPredictionCompletionDetails
    {
        public bool IsSuccessful { get; set; }
        public string? FailureInfoJson { get; set; }
        public int? DonorsPerBatch { get; set; }
    }
}
