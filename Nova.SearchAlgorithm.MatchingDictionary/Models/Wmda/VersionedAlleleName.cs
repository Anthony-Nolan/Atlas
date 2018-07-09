using System;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda
{
    public class VersionedAlleleName : IEquatable<VersionedAlleleName>
    {
        public string HlaDatabaseVersion { get; }
        public string AlleleName { get; }

        public VersionedAlleleName(string hlaDatabaseVersion, string alleleName)
        {
            HlaDatabaseVersion = hlaDatabaseVersion;
            AlleleName = alleleName;
        }

        public override string ToString()
        {
            return $"v{HlaDatabaseVersion}: {AlleleName}";
        }

        public bool Equals(VersionedAlleleName other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return 
                string.Equals(HlaDatabaseVersion, other.HlaDatabaseVersion) && 
                string.Equals(AlleleName, other.AlleleName);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((VersionedAlleleName) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (HlaDatabaseVersion.GetHashCode() * 397) ^ AlleleName.GetHashCode();
            }
        }
    }
}
