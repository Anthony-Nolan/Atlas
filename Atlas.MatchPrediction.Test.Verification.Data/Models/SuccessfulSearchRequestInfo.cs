namespace Atlas.MatchPrediction.Test.Verification.Data.Models
{
    public class SuccessfulSearchRequestInfo
    {
        public int SearchRequestRecordId { get; set; }
        public int? MatchedDonorCount { get; set; }
        public double MatchingAlgorithmTimeInMs { get; set; }
        public double MatchPredictionTimeInMs { get; set; }
        public double OverallSearchTimeInMs { get; set; }
    }
}
