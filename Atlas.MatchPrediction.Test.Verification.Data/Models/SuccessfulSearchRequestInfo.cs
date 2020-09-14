using System;

namespace Atlas.MatchPrediction.Test.Verification.Data.Models
{
    public class SuccessfulSearchRequestInfo
    {
        public int SearchRequestRecordId { get; set; }
        public int? MatchedDonorCount { get; set; }
        public TimeSpan MatchingAlgorithmTime { get; set; }
        public TimeSpan MatchPredictionTime { get; set; }
        public TimeSpan OverallSearchTime { get; set; }
    }
}
