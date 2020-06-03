using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.InternalModels.HLATypings;

namespace Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings
{
    internal interface ISerologyInfoForMatching : IMatchedOn
    {
        IEnumerable<MatchingSerology> MatchingSerologies { get; }
    }

    internal class SerologyInfoForMatching : ISerologyInfoForMatching, IEquatable<ISerologyInfoForMatching>
    {
        public HlaTyping HlaTyping { get; }
        public HlaTyping TypingUsedInMatching { get; }
        public IEnumerable<MatchingSerology> MatchingSerologies { get; }

        public SerologyInfoForMatching(
            SerologyTyping hlaTyping, 
            HlaTyping typingUsedInMatching, 
            IEnumerable<MatchingSerology> matchingSerologies)
        {
            HlaTyping = hlaTyping;
            TypingUsedInMatching = typingUsedInMatching;
            MatchingSerologies = matchingSerologies;
        }

        public override string ToString()
        {
            return $"{HlaTyping} ({TypingUsedInMatching}), " +
                   $"matchingSerologies: {string.Join("; ", MatchingSerologies.Select(m => m))}";
        }

        public bool Equals(ISerologyInfoForMatching other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return
                Equals(HlaTyping, other.HlaTyping)
                && Equals(TypingUsedInMatching, other.TypingUsedInMatching)
                && MatchingSerologies.SequenceEqual(other.MatchingSerologies);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is ISerologyInfoForMatching other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = HlaTyping.GetHashCode();
                hashCode = (hashCode * 397) ^ TypingUsedInMatching.GetHashCode();
                hashCode = (hashCode * 397) ^ MatchingSerologies.GetHashCode();
                return hashCode;
            }
        }
    }
}
