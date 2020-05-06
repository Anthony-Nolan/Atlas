using System;
using Atlas.HlaMetadataDictionary.Models.HLATypings;

namespace Atlas.HlaMetadataDictionary.Models.MatchingTypings
{
    /// <summary>
    /// Serology that matches to another HLA typing, directly or indirectly.
    /// </summary>
    public class MatchingSerology : IEquatable<MatchingSerology>
    {
        /// <summary>
        /// Typing details about the matching serology.
        /// </summary>
        public SerologyTyping SerologyTyping { get; }

        /// <summary>
        /// Does the matching serology directly map to the HLA typing
        /// it has been assigned to?
        /// </summary>
        public bool IsDirectMapping { get; }

        public MatchingSerology(SerologyTyping serologyTyping, bool isDirectMapping)
        {
            SerologyTyping = serologyTyping;
            IsDirectMapping = isDirectMapping;
        }

        public bool Equals(MatchingSerology other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return 
                SerologyTyping.Equals(other.SerologyTyping) && 
                IsDirectMapping == other.IsDirectMapping;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MatchingSerology) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (SerologyTyping.GetHashCode() * 397) ^ IsDirectMapping.GetHashCode();
            }
        }
    }
}
