using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs.Models
{
    public class FileBackedDpb1TceGroupsMetadata : IDpb1TceGroupsMetadata
    {
        public Locus Locus => Locus.Dpb1;
        public string LookupName { get; }
        public TypingMethod TypingMethod => TypingMethod.Molecular;
        public string TceGroup { get; }
        public object HlaInfoToSerialise => TceGroup;
        public string SerialisedHlaInfoType { get; }

        public FileBackedDpb1TceGroupsMetadata(string lookupName, string tceGroup, string serialisedHlaInfoType)
        {
            LookupName = lookupName;
            TceGroup = tceGroup;
            SerialisedHlaInfoType = serialisedHlaInfoType;
        }
    }
}