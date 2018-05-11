using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TypingMethod
    {
        Molecular,
        Serology
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum MolecularSubtype
    {
        NotMolecularType = 0,
        CompleteAllele = 1,
        TwoFieldAllele = 2,
        FirstFieldAllele = 3,
        NmdpCode = 4
    }

    public class MatchingDictionaryEntry : IMatchingHlaLookupResult, IEquatable<MatchingDictionaryEntry>
    {
        public string MatchLocus { get; }
        public string LookupName { get; }
        public TypingMethod TypingMethod { get; }
        public MolecularSubtype MolecularSubtype { get; }
        public SerologySubtype SerologySubtype { get; }
        public IEnumerable<string> MatchingPGroups { get; }
        public IEnumerable<SerologyEntry> MatchingSerologies { get; }

        public MatchingDictionaryEntry(
            string matchLocus,
            string lookupName,
            TypingMethod typingMethod,
            MolecularSubtype molecularSubtype,
            SerologySubtype serologySubtype,
            IEnumerable<string> matchingPGroups,
            IEnumerable<SerologyEntry> matchingSerologies
            )
        {
            MatchLocus = matchLocus;
            LookupName = lookupName;
            TypingMethod = typingMethod;
            MolecularSubtype = molecularSubtype;
            SerologySubtype = serologySubtype;
            MatchingPGroups = matchingPGroups;
            MatchingSerologies = matchingSerologies;
        }

        public bool Equals(MatchingDictionaryEntry other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return 
                string.Equals(MatchLocus, other.MatchLocus) && 
                string.Equals(LookupName, other.LookupName) && 
                TypingMethod == other.TypingMethod && 
                MolecularSubtype == other.MolecularSubtype && 
                SerologySubtype == other.SerologySubtype && 
                MatchingPGroups.SequenceEqual(other.MatchingPGroups) && 
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
                var hashCode = MatchLocus.GetHashCode();
                hashCode = (hashCode * 397) ^ LookupName.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) TypingMethod;
                hashCode = (hashCode * 397) ^ (int) MolecularSubtype;
                hashCode = (hashCode * 397) ^ (int) SerologySubtype;
                hashCode = (hashCode * 397) ^ MatchingPGroups.GetHashCode();
                hashCode = (hashCode * 397) ^ MatchingSerologies.GetHashCode();
                return hashCode;
            }
        }
    }
}
