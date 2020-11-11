using Atlas.Common.GeneticData;

namespace Atlas.MatchPrediction.Test.Verification.Models
{
    internal class CompileResultsRequest
    {
        public int VerificationRunId { get; set; }
        public int MismatchCount { get; set; }

        /// <summary>
        /// Leave null for Cross-Loci prediction.
        /// </summary>
        public Locus? Locus { get; set; }

        public string LocusName => Locus == null ? "CrossLoci" : Locus.ToString();
        public string PredictionName => $"{LocusName}_{MismatchCount}mm";

        public override string ToString()
        {
            return $"RunId: {VerificationRunId}, Prediction: {PredictionName}";
        }
    }
}
