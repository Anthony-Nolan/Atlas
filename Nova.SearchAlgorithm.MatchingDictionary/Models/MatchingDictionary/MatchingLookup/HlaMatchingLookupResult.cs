using Newtonsoft.Json;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary.MatchingLookup
{
    public class HlaMatchingLookupResult :
        IHlaMatchingLookupResult,
        IEquatable<HlaMatchingLookupResult>
    {
        public MatchLocus MatchLocus { get; }
        public string LookupName { get; }
        public TypingMethod TypingMethod { get; }
        public IEnumerable<string> MatchingPGroups { get; }

        [JsonConstructor]
        public HlaMatchingLookupResult(
            MatchLocus matchLocus,
            string lookupName,
            TypingMethod typingMethod,
            IEnumerable<string> matchingPGroups)
        {
            MatchLocus = matchLocus;
            LookupName = lookupName;
            TypingMethod = typingMethod;
            MatchingPGroups = matchingPGroups;
        }

        public HlaMatchingLookupResult(IMatchingDictionarySource<SerologyTyping> serologySource)
        {
            MatchLocus = serologySource.TypingForMatchingDictionary.MatchLocus;
            LookupName = serologySource.TypingForMatchingDictionary.Name;
            TypingMethod = TypingMethod.Serology;
            MatchingPGroups = serologySource.MatchingPGroups;
        }

        public HlaMatchingLookupResult(IMatchingDictionarySource<AlleleTyping> alleleSource, string lookupName)
        {
            MatchLocus = alleleSource.TypingForMatchingDictionary.MatchLocus;
            LookupName = lookupName;
            TypingMethod = TypingMethod.Molecular;
            MatchingPGroups = alleleSource.MatchingPGroups;
        }

        public HlaMatchingLookupResult(MatchLocus matchLocus, string lookupName, IEnumerable<HlaMatchingLookupResult> lookupResults)
        {
            var entriesList = lookupResults.ToList();

            MatchLocus = matchLocus;
            LookupName = lookupName;
            TypingMethod = TypingMethod.Molecular;
            MatchingPGroups = entriesList.SelectMany(p => p.MatchingPGroups).Distinct();
        }

        public HlaLookupTableEntity ConvertToTableEntity()
        {
            return this.ToTableEntity();
        }
        
        public bool Equals(HlaMatchingLookupResult other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return 
                MatchLocus == other.MatchLocus && 
                string.Equals(LookupName, other.LookupName) && 
                TypingMethod == other.TypingMethod && 
                MatchingPGroups.SequenceEqual(other.MatchingPGroups);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((HlaMatchingLookupResult) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int) MatchLocus;
                hashCode = (hashCode * 397) ^ LookupName.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) TypingMethod;
                hashCode = (hashCode * 397) ^ MatchingPGroups.GetHashCode();
                return hashCode;
            }
        }
    }
}
