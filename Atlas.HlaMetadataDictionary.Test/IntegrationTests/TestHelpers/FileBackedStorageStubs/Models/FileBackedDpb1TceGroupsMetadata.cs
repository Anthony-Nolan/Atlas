using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;
using Newtonsoft.Json;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs.Models
{
    public class FileBackedDpb1TceGroupsMetadata : IDpb1TceGroupsMetadata
    {
        public Locus Locus => Locus.Dpb1;
        public string LookupName { get; }
        public TypingMethod TypingMethod => TypingMethod.Molecular;
        public string TceGroup { get; }
        public string SerialisedHlaInfoType { get; }
        [JsonIgnore] // This property isn't actually used anywhere for the FileBacked objects, but it's on the interface. But we don't want to write these to file when we regenerate the FileBacked HMD's file.
        public object HlaInfoToSerialise => TceGroup;

        /// <remarks>
        /// ********************* WARNING! *********************
        /// The names of these parameters MUST stay in sync
        /// with the property names of the JSON objects found in
        /// the IntegrationTest MetadataDictionary test file.
        /// i.e. `all_hla_metadata.json`.
        /// ********************* WARNING! *********************
        /// </remarks>
        public FileBackedDpb1TceGroupsMetadata(              //*******************
            string lookupName,                               //****    See    ****
            string tceGroup,                                 //****  warning  ****
            string serialisedHlaInfoType)                    //****   above   ****
        {                                                    //*******************
            LookupName = lookupName;
            TceGroup = tceGroup;
            SerialisedHlaInfoType = serialisedHlaInfoType;
        }
    }
}