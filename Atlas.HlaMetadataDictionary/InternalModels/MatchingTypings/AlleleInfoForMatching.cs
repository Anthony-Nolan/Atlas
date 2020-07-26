using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;

namespace Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings
{
    internal interface IAlleleInfoForMatching : IMatchedOn
    {
        List<string> MatchingPGroups { get; }
        List<string> MatchingGGroups { get; }
    }

    internal class AlleleInfoForMatching : IAlleleInfoForMatching, IEquatable<IAlleleInfoForMatching>
    {
        public HlaTyping HlaTyping { get; }
        public HlaTyping TypingUsedInMatching { get; }
        public List<string> MatchingPGroups { get; }
        public List<string> MatchingGGroups { get; }

        public AlleleInfoForMatching(AlleleTyping hlaTyping, AlleleTyping typingUsedInMatching, List<string> pGroup, List<string> gGroup)
        {
            HlaTyping = hlaTyping;
            TypingUsedInMatching = typingUsedInMatching;
            MatchingPGroups = pGroup;
            MatchingGGroups = gGroup;
        }

        public bool Equals(IAlleleInfoForMatching other)
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
            return Equals((IAlleleInfoForMatching) obj);
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
