using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings
{
    public interface IAlleleInfoForMatching : IMatchedOn, IMatchingPGroups, IMatchingGGroups
    {
        
    }

    public class AlleleInfoForMatching : IAlleleInfoForMatching, IEquatable<AlleleInfoForMatching>
    {
        public HlaTyping HlaTyping { get; }
        public HlaTyping TypingUsedInMatching { get; }
        public IEnumerable<string> MatchingPGroups { get; }
        public IEnumerable<string> MatchingGGroups { get; }

        public AlleleInfoForMatching(AlleleTyping hlaTyping, AlleleTyping typingUsedInMatching, IEnumerable<string> pGroup, IEnumerable<string> gGroup)
        {
            HlaTyping = hlaTyping;
            TypingUsedInMatching = typingUsedInMatching;
            MatchingPGroups = pGroup;
            MatchingGGroups = gGroup;
        }

        public bool Equals(AlleleInfoForMatching other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return
                HlaTyping.Equals(other.HlaTyping) && 
                TypingUsedInMatching.Equals(other.TypingUsedInMatching) && 
                MatchingPGroups.SequenceEqual(other.MatchingPGroups) && 
                MatchingGGroups.SequenceEqual(other.MatchingGGroups);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AlleleInfoForMatching) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = HlaTyping.GetHashCode();
                hashCode = (hashCode * 397) ^ TypingUsedInMatching.GetHashCode();
                hashCode = (hashCode * 397) ^ MatchingPGroups.GetHashCode();
                hashCode = (hashCode * 397) ^ MatchingGGroups.GetHashCode();
                return hashCode;
            }
        }
    }
}
