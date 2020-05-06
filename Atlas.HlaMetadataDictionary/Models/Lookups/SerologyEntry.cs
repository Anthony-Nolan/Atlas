using System;
using Newtonsoft.Json;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Models.Lookups
{
    public class SerologyEntry : IEquatable<SerologyEntry>
    {
        // Shortened property names are used when serialising the object for storage
        // to reduce the total row size

        public string Name { get; }

        [JsonProperty("subtype")]
        public SerologySubtype SerologySubtype { get; }

        [JsonProperty("direct")]
        public bool IsDirectMapping { get; }

        public SerologyEntry(string name, SerologySubtype serologySubtype, bool isDirectMapping)
        {
            Name = name;
            SerologySubtype = serologySubtype;
            IsDirectMapping = isDirectMapping;
        }

        public bool Equals(SerologyEntry other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return 
                string.Equals(Name, other.Name) && 
                SerologySubtype == other.SerologySubtype && 
                IsDirectMapping == other.IsDirectMapping;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SerologyEntry) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Name.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) SerologySubtype;
                hashCode = (hashCode * 397) ^ IsDirectMapping.GetHashCode();
                return hashCode;
            }
        }
    }
}
