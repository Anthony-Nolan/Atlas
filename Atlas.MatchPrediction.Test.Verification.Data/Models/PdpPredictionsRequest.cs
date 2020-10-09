using Atlas.Common.GeneticData;

namespace Atlas.MatchPrediction.Test.Verification.Data.Models
{
    public class PdpPredictionsRequest
    {
        public int VerificationRunId { get; set; }

        /// <summary>
        /// Prediction mismatch count, e.g., if you want P(0 mismatches), then set to 0.
        /// </summary>
        public int MismatchCount { get; set; }

        /// <summary>
        /// Leave null for Cross-Loci prediction.
        /// </summary>
        public Locus? Locus { get; set; }
    }
}
