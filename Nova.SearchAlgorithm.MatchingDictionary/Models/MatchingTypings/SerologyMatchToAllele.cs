using System;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings
{
    public class SerologyMatchToAllele : IEquatable<SerologyMatchToAllele>
    {
        public SerologyTyping SerologyTyping { get; }
        public bool IsUnexpected { get; set; }

        public SerologyMatchToAllele(SerologyTyping serologyTyping, bool isUnexpected = false)
        {
            SerologyTyping = serologyTyping;
            IsUnexpected = isUnexpected;
        }

        public bool Equals(SerologyMatchToAllele other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return SerologyTyping.Equals(other.SerologyTyping) && IsUnexpected == other.IsUnexpected;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SerologyMatchToAllele) obj);
        }

        public override int GetHashCode()
        {
            return SerologyTyping.GetHashCode();
        }
    }
}
