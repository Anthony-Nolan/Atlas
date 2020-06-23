using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;

namespace Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata
{
    /// <summary>
    /// Metadata required to match HLA pairings.
    /// </summary>
    public interface IHlaMatchingMetadata : ISerialisableHlaMetadata
    {
        IEnumerable<string> MatchingPGroups { get; }
        bool IsNullExpressingTyping { get; }
    }

    internal class HlaMatchingMetadata :
        SerialisableHlaMetadata,
        IHlaMatchingMetadata,
        IEquatable<HlaMatchingMetadata>
    {
        public IEnumerable<string> MatchingPGroups { get; }
        public bool IsNullExpressingTyping => TypingMethod == TypingMethod.Molecular && !MatchingPGroups.Any();
        public override object HlaInfoToSerialise => MatchingPGroups.ToList(); //Needs to be reified for deserialisation Type validation

        internal HlaMatchingMetadata(
            Locus locus,
            string lookupName,
            TypingMethod typingMethod,
            IEnumerable<string> matchingPGroups)
        : base(locus, lookupName, typingMethod)
        {
            MatchingPGroups = matchingPGroups;
        }

        public bool Equals(HlaMatchingMetadata other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return
                Locus == other.Locus &&
                string.Equals(LookupName, other.LookupName) &&
                TypingMethod == other.TypingMethod &&
                MatchingPGroups.SequenceEqual(other.MatchingPGroups);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((HlaMatchingMetadata)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)Locus;
                hashCode = (hashCode * 397) ^ LookupName.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)TypingMethod;
                hashCode = (hashCode * 397) ^ MatchingPGroups.GetHashCode();
                return hashCode;
            }
        }
    }
}
