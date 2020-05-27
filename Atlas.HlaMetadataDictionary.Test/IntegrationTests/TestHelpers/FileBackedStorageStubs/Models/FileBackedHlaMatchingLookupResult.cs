using System.Collections.Generic;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.Models.Lookups.MatchingLookup;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs.Models
{
    public class FileBackedHlaMatchingLookupResult : IHlaMatchingLookupResult
    {
        public Locus Locus { get; }
        public string LookupName { get; }
        public TypingMethod TypingMethod { get; }
        public object HlaInfoToSerialise => MatchingPGroups;
        public IEnumerable<string> MatchingPGroups { get; }
        public bool IsNullExpressingTyping { get; }

        public FileBackedHlaMatchingLookupResult(
            Locus locus,
            string lookupName,
            TypingMethod typingMethod,
            List<string> matchingPGroups, 
            bool isNullExpressingTyping)
        {
            Locus = locus;
            LookupName = lookupName;
            TypingMethod = typingMethod;
            MatchingPGroups = matchingPGroups;
            IsNullExpressingTyping = isNullExpressingTyping;
        }
    }
}
