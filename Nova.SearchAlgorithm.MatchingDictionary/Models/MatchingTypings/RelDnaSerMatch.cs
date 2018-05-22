using System;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings
{
    public class RelDnaSerMatch : IEquatable<RelDnaSerMatch>
    {
        public SerologyTyping SerologyTyping { get; }
        public bool IsUnexpected { get; set; }

        public RelDnaSerMatch(SerologyTyping serologyTyping, bool isUnexpected = false)
        {
            SerologyTyping = serologyTyping;
            IsUnexpected = isUnexpected;
        }

        public bool Equals(RelDnaSerMatch other)
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
            return Equals((RelDnaSerMatch) obj);
        }

        public override int GetHashCode()
        {
            return SerologyTyping.GetHashCode();
        }
    }
}
