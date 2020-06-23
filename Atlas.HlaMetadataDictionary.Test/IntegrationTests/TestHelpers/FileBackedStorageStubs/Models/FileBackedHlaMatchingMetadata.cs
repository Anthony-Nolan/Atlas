using System.Collections.Generic;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs.Models
{
    public class FileBackedHlaMatchingMetadata : IHlaMatchingMetadata
    {
        public Locus Locus { get; }
        public string LookupName { get; }
        public TypingMethod TypingMethod { get; }
        public object HlaInfoToSerialise => MatchingPGroups;
        public string SerialisedHlaInfoType { get; }
        public IEnumerable<string> MatchingPGroups { get; }
        public bool IsNullExpressingTyping { get; }

        public FileBackedHlaMatchingMetadata(
            Locus locus,
            string lookupName,
            TypingMethod typingMethod,
            List<string> matchingPGroups, 
            bool isNullExpressingTyping,
            string serialisedHlaInfoType)
        {
            Locus = locus;
            LookupName = lookupName;
            TypingMethod = typingMethod;
            MatchingPGroups = matchingPGroups;
            IsNullExpressingTyping = isNullExpressingTyping;
            SerialisedHlaInfoType = serialisedHlaInfoType;
        }
    }
}
