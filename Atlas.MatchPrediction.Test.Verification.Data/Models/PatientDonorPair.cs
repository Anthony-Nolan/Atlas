using System;

namespace Atlas.MatchPrediction.Test.Verification.Data.Models
{
    public class PatientDonorPair
    {
        public int PatientGenotypeSimulantId { get; set; }
        public int DonorGenotypeSimulantId { get; set; }
    }

    public class PdpPrediction : PatientDonorPair
    {
        public decimal Probability { get; set; }

        public int ProbabilityAsRoundedPercentage => (int)Math.Round(100 * Probability);
    }
}