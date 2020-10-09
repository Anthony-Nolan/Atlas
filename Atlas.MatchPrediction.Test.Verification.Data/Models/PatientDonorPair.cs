using System;

namespace Atlas.MatchPrediction.Test.Verification.Data.Models
{
    public class PatientDonorPair : IEquatable<PatientDonorPair>
    {
        public int PatientGenotypeSimulantId { get; set; }
        public int DonorGenotypeSimulantId { get; set; }

        public bool Equals(PatientDonorPair other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return PatientGenotypeSimulantId == other.PatientGenotypeSimulantId && DonorGenotypeSimulantId == other.DonorGenotypeSimulantId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PatientDonorPair) obj);
        }

        public override int GetHashCode() => HashCode.Combine(PatientGenotypeSimulantId, DonorGenotypeSimulantId);
    }

    public class PdpPrediction : PatientDonorPair
    {
        public decimal Probability { get; set; }

        public int ProbabilityAsRoundedPercentage => (int)Math.Round(100 * Probability);
    }
}