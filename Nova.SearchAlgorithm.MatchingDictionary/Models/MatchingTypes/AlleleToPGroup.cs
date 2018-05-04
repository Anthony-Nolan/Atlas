using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes
{
    public interface IAlleleToPGroup : IMatchingPGroups, IMatchedOn
    {
        
    }

    public class AlleleToPGroup : IAlleleToPGroup, IEquatable<IAlleleToPGroup>
    {
        public HlaType HlaType { get; }
        public HlaType TypeUsedInMatching { get; }
        public IEnumerable<string> MatchingPGroups { get; }

        public AlleleToPGroup(Allele hlaType, Allele typeUsedInMatching, IEnumerable<string> pGroups)
        {
            HlaType = hlaType;
            TypeUsedInMatching = typeUsedInMatching;
            MatchingPGroups = pGroups;
        }

        public override string ToString()
        {
            return $"{HlaType} ({TypeUsedInMatching}), " +
                   $"matchingPGroup: {string.Join("/", MatchingPGroups.Select(m => m))}";
        }

        public bool Equals(IAlleleToPGroup other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return
                Equals(HlaType, other.HlaType)
                && Equals(TypeUsedInMatching, other.TypeUsedInMatching)
                && MatchingPGroups.SequenceEqual(other.MatchingPGroups);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is IAlleleToPGroup other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (HlaType != null ? HlaType.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ TypeUsedInMatching.GetHashCode();
                hashCode = (hashCode * 397) ^ MatchingPGroups.GetHashCode();
                return hashCode;
            }
        }
    }
}
