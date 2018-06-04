using System;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings
{
    public class SerologyMatch : IEquatable<SerologyMatch>
    {
        public SerologyTyping SerologyTyping { get; }
        public bool IsUnexpected { get; set; }

        public SerologyMatch(SerologyTyping serologyTyping, bool isUnexpected = false)
        {
            SerologyTyping = serologyTyping;
            IsUnexpected = isUnexpected;
        }

        public bool Equals(SerologyMatch other)
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
            return Equals((SerologyMatch) obj);
        }

        public override int GetHashCode()
        {
            return SerologyTyping.GetHashCode();
        }
    }
}
