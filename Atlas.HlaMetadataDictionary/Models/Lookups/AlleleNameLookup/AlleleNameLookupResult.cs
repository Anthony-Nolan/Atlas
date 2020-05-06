using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.MatchingDictionary.HlaTypingInfo;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.HLATypings;
using Atlas.MatchingAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Models.Lookups.AlleleNameLookup
{
    public interface IAlleleNameLookupResult : IHlaLookupResult
    {
        IEnumerable<string> CurrentAlleleNames { get; }
    }

    public class AlleleNameLookupResult : 
        IAlleleNameLookupResult, 
        IEquatable<AlleleNameLookupResult>
    {
        public Locus Locus { get; }
        public string LookupName { get; }
        public TypingMethod TypingMethod => TypingMethod.Molecular;
        public IEnumerable<string> CurrentAlleleNames { get; }
        public object HlaInfoToSerialise => CurrentAlleleNames;

        public AlleleNameLookupResult(Locus locus, string lookupName, IEnumerable<string> currentAlleleNames)
        {
            Locus = locus;
            LookupName = lookupName;
            CurrentAlleleNames = currentAlleleNames;
        }

        public AlleleNameLookupResult(string locus, string lookupName, string currentAlleleName)
        {
            Locus = MatchingDictionaryLoci.GetLocusFromTypingLocusNameIfExists(TypingMethod.Molecular, locus);
            LookupName = lookupName;
            CurrentAlleleNames = new[] {currentAlleleName};
        }

        public HlaLookupTableEntity ConvertToTableEntity()
        {
            return new HlaLookupTableEntity(this);
        }

        public bool Equals(AlleleNameLookupResult other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return 
                Locus == other.Locus && 
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
                var hashCode = (int) Locus;
                hashCode = (hashCode * 397) ^ LookupName.GetHashCode();
                hashCode = (hashCode * 397) ^ CurrentAlleleNames.GetHashCode();
                return hashCode;
            }
        }
    }
}
