namespace Atlas.Search.Tracking.Models
{
    public class MatchingAlgorithmCompletedEvent
    {
        public int SearchRequestId { get; set; }
        public byte AttemptNumber { get; set; }
        public DateTime CompletionTimeUtc { get; set; }
        public bool MatchingAlgorithm_IsSuccessful { get; set; }
        public string MatchingAlgorithm_FailureInfo_Json { get; set; }
        public byte MatchingAlgorithm_TotalAttemptsNumber { get; set; }
        public int MatchingAlgorithm_NumberOfMatching { get; set; }
        public int MatchingAlgorithm_NumberOfNoLongerMatching { get; set; }
        public int MatchingAlgorithm_NumberOfResults { get; set; }
        public string MatchingAlgorithm_HlaNomenclatureVersion { get; set; }
        public bool MatchingAlgorithm_ResultsSent { get; set; }
        public DateTime? MatchingAlgorithm_ResultsSentTimeUTC { get; set; }
    }
}
