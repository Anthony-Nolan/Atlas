using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings
{
    public interface IAlleleInfoForMatching : IMatchedOn, IMatchingPGroups
    {
        
    }

    public class AlleleInfoForMatching : IAlleleInfoForMatching, IEquatable<IAlleleInfoForMatching>
    {
        public HlaTyping HlaTyping { get; }
        public HlaTyping TypingUsedInMatching { get; }
        public IEnumerable<string> MatchingPGroups { get; }

        public AlleleInfoForMatching(AlleleTyping hlaTyping, AlleleTyping typingUsedInMatching, IEnumerable<string> pGroups)
        {
            HlaTyping = hlaTyping;
            TypingUsedInMatching = typingUsedInMatching;
            MatchingPGroups = pGroups;
        }

        public override string ToString()
        {
            return $"{HlaTyping} ({TypingUsedInMatching}), " +
                   $"matchingPGroup: {string.Join("/", MatchingPGroups.Select(m => m))}";
        }

        public bool Equals(IAlleleInfoForMatching other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return
                Equals(HlaTyping, other.HlaTyping)
                && Equals(TypingUsedInMatching, other.TypingUsedInMatching)
                && MatchingPGroups.SequenceEqual(other.MatchingPGroups);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is IAlleleInfoForMatching other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (HlaTyping != null ? HlaTyping.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ TypingUsedInMatching.GetHashCode();
                hashCode = (hashCode * 397) ^ MatchingPGroups.GetHashCode();
                return hashCode;
            }
        }
    }
}
