using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.HlaMetadataDictionary.Models.HLATypings;
using Atlas.HlaMetadataDictionary.Models.Lookups.MatchingLookup;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;
using System.Collections.Generic;

namespace Atlas.MatchingAlgorithm.Test.Integration.Storage.FileBackedHlaLookupRepositories.Models
{
    public class FileBackedHlaMatchingLookupResult : IHlaMatchingLookupResult
    {
        public Locus Locus { get; }
        public string LookupName { get; }
        public TypingMethod TypingMethod { get; }
        public object HlaInfoToSerialise { get; }
        public IEnumerable<string> MatchingPGroups { get; }
        public bool IsNullExpressingTyping { get; }

        public FileBackedHlaMatchingLookupResult(
            Locus locus,
            string lookupName,
            TypingMethod typingMethod,
            IEnumerable<string> matchingPGroups, 
            bool isNullExpressingTyping)
        {
            Locus = locus;
            LookupName = lookupName;
            TypingMethod = typingMethod;
            HlaInfoToSerialise = matchingPGroups;
            MatchingPGroups = matchingPGroups;
            IsNullExpressingTyping = isNullExpressingTyping;
        }

        public HlaLookupTableEntity ConvertToTableEntity()
        {
            return new HlaLookupTableEntity(this);
        }
    }
}
