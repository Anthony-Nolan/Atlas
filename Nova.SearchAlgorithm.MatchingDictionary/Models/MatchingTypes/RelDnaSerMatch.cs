using System;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes
{
    public class RelDnaSerMatch : IEquatable<RelDnaSerMatch>
    {
        public Serology Serology { get; }
        public bool IsUnexpected { get; set; }

        public RelDnaSerMatch(Serology serology, bool isUnexpected = false)
        {
            Serology = serology;
            IsUnexpected = isUnexpected;
        }

        public bool Equals(RelDnaSerMatch other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Serology.Equals(other.Serology) && IsUnexpected == other.IsUnexpected;
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
            return Serology.GetHashCode();
        }
    }
}
