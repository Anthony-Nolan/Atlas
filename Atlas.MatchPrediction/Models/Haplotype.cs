using System.Collections.Generic;
using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.Models
{
    public class Haplotype
    {
        public LociInfo<string> Hla { get; set; }
        public decimal Frequency { get; set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((Haplotype)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = EqualityComparer<LociInfo<string>>.Default.GetHashCode(Hla);
                hashCode = (hashCode * 397) ^ EqualityComparer<decimal>.Default.GetHashCode(Frequency);
                return hashCode;
            }
        }

        private bool Equals(Haplotype other)
        {
            return other.Hla == Hla && other.Frequency == Frequency;
        }
    }
}
