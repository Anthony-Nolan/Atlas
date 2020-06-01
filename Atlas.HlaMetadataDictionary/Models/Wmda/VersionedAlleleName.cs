using System;

namespace Atlas.HlaMetadataDictionary.Models.Wmda
{
    internal class VersionedAlleleName : IEquatable<VersionedAlleleName>
    {
        public string HlaNomenclatureVersion { get; }
        public string AlleleName { get; }

        public VersionedAlleleName(string hlaNomenclatureVersion, string alleleName)
        {
            HlaNomenclatureVersion = hlaNomenclatureVersion;
            AlleleName = alleleName;
        }

        public override string ToString()
        {
            return $"v{HlaNomenclatureVersion}: {AlleleName}";
        }

        #region IEquatable
        public bool Equals(VersionedAlleleName other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return 
                string.Equals(HlaNomenclatureVersion, other.HlaNomenclatureVersion) && 
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
                return (HlaNomenclatureVersion.GetHashCode() * 397) ^ AlleleName.GetHashCode();
            }
        }
        #endregion
    }
}
