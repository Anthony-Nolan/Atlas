using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes
{
    public interface ISerologyToSerology : IMatchedOn, IMatchingSerologies
    {
        
    }

    public class SerologyToSerology : ISerologyToSerology, IEquatable<ISerologyToSerology>
    {
        public HlaType HlaType { get; }
        public HlaType TypeUsedInMatching { get; }
        public IEnumerable<Serology> MatchingSerologies { get; }

        public SerologyToSerology(Serology hlaType, HlaType typeUsedInMatching, IEnumerable<Serology> matchingSerologies)
        {
            HlaType = hlaType;
            TypeUsedInMatching = typeUsedInMatching;
            MatchingSerologies = matchingSerologies;
        }

        public override string ToString()
        {
            return $"{HlaType} ({TypeUsedInMatching}), " +
                   $"matchingSerologies: {string.Join("; ", MatchingSerologies.Select(m => m))}";
        }

        public bool Equals(ISerologyToSerology other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return
                Equals(HlaType, other.HlaType)
                && Equals(TypeUsedInMatching, other.TypeUsedInMatching)
                && MatchingSerologies.SequenceEqual(other.MatchingSerologies);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is ISerologyToSerology other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = HlaType.GetHashCode();
                hashCode = (hashCode * 397) ^ TypeUsedInMatching.GetHashCode();
                hashCode = (hashCode * 397) ^ MatchingSerologies.GetHashCode();
                return hashCode;
            }
        }
    }
}
