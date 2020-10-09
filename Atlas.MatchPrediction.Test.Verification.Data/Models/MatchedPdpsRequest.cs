using Atlas.Common.GeneticData;

namespace Atlas.MatchPrediction.Test.Verification.Data.Models
{
    public class MatchedPdpsRequest
    {
        public int VerificationRunId { get; set; }
        public int MatchCount { get; set; }
    }

    public class SingleLocusMatchedPdpsRequest : MatchedPdpsRequest
    {
        public Locus Locus { get; set; }
    }
}