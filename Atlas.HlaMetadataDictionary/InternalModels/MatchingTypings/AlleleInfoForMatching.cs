using System;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;

namespace Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings
{
    internal class AlleleInfoForMatching : IMatchedOn, IEquatable<AlleleInfoForMatching>
    {
        public HlaTyping HlaTyping { get; }
        public HlaTyping TypingUsedInMatching { get; }
        public string MatchingPGroup { get; }
        public string MatchingGGroup { get; }

        public AlleleInfoForMatching(AlleleTyping hlaTyping, AlleleTyping typingUsedInMatching, string pGroup, string gGroup)
        {
            HlaTyping = hlaTyping;
            TypingUsedInMatching = typingUsedInMatching;
            MatchingPGroup = pGroup;
            MatchingGGroup = gGroup;
        }

        public bool Equals(AlleleInfoForMatching other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return
                HlaTyping.Equals(other.HlaTyping) && 
                TypingUsedInMatching.Equals(other.TypingUsedInMatching) && 
                MatchingPGroup.Equals(other.MatchingPGroup) && 
                MatchingGGroup.Equals(other.MatchingGGroup);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals((AlleleInfoForMatching) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = HlaTyping.GetHashCode();
                hashCode = (hashCode * 397) ^ TypingUsedInMatching.GetHashCode();
                hashCode = (hashCode * 397) ^ MatchingPGroup.GetHashCode();
                hashCode = (hashCode * 397) ^ MatchingGGroup.GetHashCode();
                return hashCode;
            }
        }
    }
}