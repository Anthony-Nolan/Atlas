using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.MatchingLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Test.Integration.Storage.FileBackedHlaLookupRepositories.Models
{
    public class FileBackedHlaMatchingLookupResult : IHlaMatchingLookupResult
    {
        public MatchLocus MatchLocus { get; }
        public string LookupName { get; }
        public TypingMethod TypingMethod { get; }
        public object HlaInfoToSerialise { get; }
        public IEnumerable<string> MatchingPGroups { get; }

        public FileBackedHlaMatchingLookupResult(
            MatchLocus matchLocus,
            string lookupName,
            TypingMethod typingMethod,
            IEnumerable<string> matchingPGroups)
        {
            MatchLocus = matchLocus;
            LookupName = lookupName;
            TypingMethod = typingMethod;
            HlaInfoToSerialise = matchingPGroups;
            MatchingPGroups = matchingPGroups;
        }

        public HlaLookupTableEntity ConvertToTableEntity()
        {
            return this.ToTableEntity();
        }
    }
}
