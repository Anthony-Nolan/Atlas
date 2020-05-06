using System;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Models.Wmda
{
    public class ConfidentialAllele : IWmdaHlaTyping, IEquatable<IWmdaHlaTyping>
    {
        public TypingMethod TypingMethod => TypingMethod.Molecular;
        public string TypingLocus { get; set; }
        public string Name { get; set; }

        public ConfidentialAllele(string locus, string name)
        {
            TypingLocus = locus;
            Name = name;
        }

        public override string ToString()
        {
            return $"locus: {TypingLocus}, name: {Name}";
        }

        public bool Equals(IWmdaHlaTyping other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return 
                string.Equals(TypingLocus, other.TypingLocus) 
                && string.Equals(Name, other.Name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is IWmdaHlaTyping other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (TypingLocus.GetHashCode() * 397) ^ Name.GetHashCode();
            }
        }
    }
}
