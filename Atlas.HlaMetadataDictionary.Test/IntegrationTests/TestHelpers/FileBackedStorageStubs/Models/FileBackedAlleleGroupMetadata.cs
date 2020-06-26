using System.Collections.Generic;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs.Models
{
    public class FileBackedAlleleGroupMetadata : IAlleleGroupMetadata
    {
        public Locus Locus { get; }
        public string LookupName { get; }
        public TypingMethod TypingMethod { get; }
        public object HlaInfoToSerialise { get; }
        public string SerialisedHlaInfoType { get; }
        public List<string> AllelesInGroup { get; }

        public FileBackedAlleleGroupMetadata(Locus locus, string lookupName, TypingMethod typingMethod, object hlaInfoToSerialise, string serialisedHlaInfoType, List<string> allelesInGroup)
        {
            Locus = locus;
            LookupName = lookupName;
            TypingMethod = typingMethod;
            HlaInfoToSerialise = hlaInfoToSerialise;
            SerialisedHlaInfoType = serialisedHlaInfoType;
            AllelesInGroup = allelesInGroup;
        }
    }
}