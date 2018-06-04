using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary
{
    /// <summary>
    /// HLA data held within the matching dictionary.
    /// Properties are optimised for dictionary lookups.
    /// </summary>
    public class MatchingDictionaryEntry : IMatchingHlaLookupResult, IEquatable<MatchingDictionaryEntry>
    {
        public MatchLocus MatchLocus { get; }
        public string LookupName { get; }
        public TypingMethod TypingMethod { get; }
        public MolecularSubtype MolecularSubtype { get; }
        public SerologySubtype SerologySubtype { get; }
        public IEnumerable<string> MatchingPGroups { get; }
        public IEnumerable<string> MatchingGGroups { get; }
        public IEnumerable<SerologyEntry> MatchingSerologies { get; }

        public MatchingDictionaryEntry(
            MatchLocus matchLocus,
            string lookupName,
            TypingMethod typingMethod,
            MolecularSubtype molecularSubtype,
            SerologySubtype serologySubtype,
            IEnumerable<string> matchingPGroups,
            IEnumerable<string> matchingGGroups,
            IEnumerable<SerologyEntry> matchingSerologies)
        {
            MatchLocus = matchLocus;
            LookupName = lookupName;
            TypingMethod = typingMethod;
            MolecularSubtype = molecularSubtype;
            SerologySubtype = serologySubtype;
            MatchingPGroups = matchingPGroups;
            MatchingGGroups = matchingGGroups;
            MatchingSerologies = matchingSerologies;
        }
        
        public bool Equals(MatchingDictionaryEntry other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return 
                MatchLocus == other.MatchLocus && 
                string.Equals(LookupName, other.LookupName) && 
                TypingMethod == other.TypingMethod &&
                MolecularSubtype == other.MolecularSubtype &&
                SerologySubtype == other.SerologySubtype &&
                MatchingPGroups.SequenceEqual(other.MatchingPGroups) &&
                MatchingGGroups.SequenceEqual(other.MatchingGGroups) &&
                MatchingSerologies.SequenceEqual(other.MatchingSerologies);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MatchingDictionaryEntry) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int) MatchLocus;
                hashCode = (hashCode * 397) ^ LookupName.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) TypingMethod;
                hashCode = (hashCode * 397) ^ (int) MolecularSubtype;
                hashCode = (hashCode * 397) ^ (int) SerologySubtype;
                hashCode = (hashCode * 397) ^ MatchingPGroups.GetHashCode();
                hashCode = (hashCode * 397) ^ MatchingGGroups.GetHashCode();
                hashCode = (hashCode * 397) ^ MatchingSerologies.GetHashCode();
                return hashCode;
            }
        }
    }
}
