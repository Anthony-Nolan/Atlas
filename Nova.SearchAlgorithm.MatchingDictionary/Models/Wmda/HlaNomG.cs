using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda
{
    public class HlaNomG : IWmdaAlleleGroup, IEquatable<HlaNomG>
    {
        public TypingMethod TypingMethod => TypingMethod.Molecular;
        public string Locus { get; set; }
        public string Name { get; set; }
        public IEnumerable<string> Alleles { get; set; }

        public HlaNomG()
        {            
        }

        public HlaNomG(string wmdaLocus, string name, IEnumerable<string> alleles)
        {
            Locus = wmdaLocus;
            Name = name;
            Alleles = alleles;
        }

        public override string ToString()
        {
            return $"locus: {Locus}, gGroup: {Name}, alleles: {string.Join("/", Alleles)}";
        }

        public bool Equals(HlaNomG other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return 
                string.Equals(Locus, other.Locus) 
                && string.Equals(Name, other.Name) 
                && Alleles.SequenceEqual(other.Alleles);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((HlaNomG) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Locus.GetHashCode();
                hashCode = (hashCode * 397) ^ Name.GetHashCode();
                hashCode = (hashCode * 397) ^ Alleles.GetHashCode();
                return hashCode;
            }
        }
    }
}
