namespace Atlas.MatchPrediction.Test.Verification.Models
{
    internal class VerificationResult
    {
        public int Probability { get; set; }
        public int ActuallyMatchedPdpCount { get; set; }
        public int TotalPdpCount { get; set; }
    }
}
