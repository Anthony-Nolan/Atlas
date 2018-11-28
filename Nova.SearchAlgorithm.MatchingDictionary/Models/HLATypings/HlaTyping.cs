using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using System;
using Nova.SearchAlgorithm.MatchingDictionary.HlaTypingInfo;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings
{
    public class HlaTyping : IEquatable<HlaTyping>, IWmdaHlaTyping
    {
        public TypingMethod TypingMethod { get; }
        public string Locus { get; set; }
        public MatchLocus MatchLocus { get; }
        public string Name { get; set; }
        public bool IsDeleted { get; }

        public HlaTyping(TypingMethod typingMethod, string locus, string name, bool isDeleted = false)
        {
            Locus = locus;
            Name = name;
            TypingMethod = typingMethod;
            IsDeleted = isDeleted;
            MatchLocus = MatchingDictionaryLoci.GetMatchLocusFromTypingLocusIfExists(typingMethod, locus);
        }

        public override string ToString()
        {
            return $"{Locus}{Name}";
        }
        
        public bool Equals(HlaTyping other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return 
                TypingMethod == other.TypingMethod && 
                string.Equals(Locus, other.Locus) && 
                MatchLocus == other.MatchLocus && 
                string.Equals(Name, other.Name) && 
                IsDeleted == other.IsDeleted;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((HlaTyping) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int) TypingMethod;
                hashCode = (hashCode * 397) ^ Locus.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) MatchLocus;
                hashCode = (hashCode * 397) ^ Name.GetHashCode();
                hashCode = (hashCode * 397) ^ IsDeleted.GetHashCode();
                return hashCode;
            }
        }
    }
}
