using System.Text.Json.Serialization;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs.Models
{
    public class FileBackedGGroupToPGroupMetadata : IGGroupToPGroupMetadata
    {
        public Locus Locus { get; }
        public string LookupName { get; }
        public TypingMethod TypingMethod => TypingMethod.Molecular;
        public string PGroup { get; }
        public string SerialisedHlaInfoType { get; }
        [JsonIgnore] // This property isn't actually used anywhere for the FileBacked objects, but it's on the interface. But we don't want to write these to file when we regenerate the FileBacked HMD's file.
        public object HlaInfoToSerialise => PGroup;

        /// <remarks>
        /// ********************* WARNING! *********************
        /// The names of these parameters MUST stay in sync
        /// with the property names of the JSON objects found in
        /// the IntegrationTest MetadataDictionary test file.
        /// i.e. `all_hla_metadata.json`.
        /// ********************* WARNING! *********************
        /// </remarks>
        public FileBackedGGroupToPGroupMetadata(         //*******************
            Locus locus,                                 //****  warning  ****
            string lookupName,                           //****    See    ***
            string pGroup,                               //****   above   ****
            string serialisedHlaInfoType)                //*******************
        {
            Locus = locus;
            LookupName = lookupName;
            PGroup = pGroup;
            SerialisedHlaInfoType = serialisedHlaInfoType;
        }
    }
}
