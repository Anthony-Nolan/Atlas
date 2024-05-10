namespace Atlas.Search.Tracking.Models
{
    public class MatchPredictionCompletedEvent
    {
        public int SearchRequestId { get; set; }
        public DateTime CompletionTimeUtc { get; set; }
        public bool MatchPrediction_IsSuccessful { get; set; }
        public string MatchPrediction_FailureInfo_Json { get; set; }
        public int MatchPrediction_DonorsPerBatch { get; set; }
        public int MatchPrediction_TotalNumberOfBatches { get; set; }
    }
}
