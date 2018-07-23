using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.HlaTypingInfo;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.AlleleNameLookup
{
    public class AlleleNameLookupResult : 
        IHlaLookupResult, 
        IEquatable<AlleleNameLookupResult>
    {
        public MatchLocus MatchLocus { get; }
        public string LookupName { get; }
        public TypingMethod TypingMethod => TypingMethod.Molecular;
        public IEnumerable<string> CurrentAlleleNames { get; }
        public object HlaInfoToSerialise => CurrentAlleleNames;

        public AlleleNameLookupResult(MatchLocus matchLocus, string lookupName, IEnumerable<string> currentAlleleNames)
        {
            MatchLocus = matchLocus;
            LookupName = lookupName;
            CurrentAlleleNames = currentAlleleNames;
        }

        public AlleleNameLookupResult(string locus, string lookupName, string currentAlleleName)
        {
            MatchLocus = PermittedLocusNames.GetMatchLocusNameFromTypingLocusIfExists(TypingMethod.Molecular, locus);
            LookupName = lookupName;
            CurrentAlleleNames = new[] {currentAlleleName};
        }

        public HlaLookupTableEntity ConvertToTableEntity()
        {
            return this.ToTableEntity();
        }

        public bool Equals(AlleleNameLookupResult other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return 
                MatchLocus == other.MatchLocus && 
                string.Equals(LookupName, other.LookupName) && 
                CurrentAlleleNames.SequenceEqual(other.CurrentAlleleNames);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AlleleNameLookupResult) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int) MatchLocus;
                hashCode = (hashCode * 397) ^ LookupName.GetHashCode();
                hashCode = (hashCode * 397) ^ CurrentAlleleNames.GetHashCode();
                return hashCode;
            }
        }
    }
}
