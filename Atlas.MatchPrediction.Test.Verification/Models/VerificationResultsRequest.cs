﻿namespace Atlas.MatchPrediction.Test.Verification.Models
{
    public class VerificationResultsRequest
    {
        public int VerificationRunId { get; set; }

        /// <summary>
        /// Prediction mismatch count, e.g., if you want P(0 mismatches), then set to 0.
        /// </summary>
        public int MismatchCount { get; set; }

        /// <summary>
        /// Directory where results file should be written to.
        /// Should not include filename, as this will be autogenerated.
        /// </summary>
        public string WriteDirectory { get; set; }
    }
}