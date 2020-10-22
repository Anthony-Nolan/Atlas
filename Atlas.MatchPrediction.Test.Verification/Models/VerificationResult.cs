using System.Collections.Generic;

namespace Atlas.MatchPrediction.Test.Verification.Models
{
    internal class VerificationResult
    {
        public IEnumerable<ActualVersusExpectedResult> ActualVersusExpectedResults { get; set; }
    }

    internal class ActualVersusExpectedResult
    {
        public int Probability { get; set; }
        public int ActuallyMatchedPdpCount { get; set; }
        public int TotalPdpCount { get; set; }
    }
}
