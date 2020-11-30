using System.Collections.Generic;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;
using Newtonsoft.Json;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs.Models
{
    public class FileBackedSmallGGroupsMetadata : ISmallGGroupsMetadata
    {
        public Locus Locus { get; }
        public string LookupName { get; }
        public TypingMethod TypingMethod => TypingMethod.Molecular;
        public List<string> SmallGGroups { get; }
        public string SerialisedHlaInfoType { get; }

        [JsonIgnore] // This property isn't actually used anywhere for the FileBacked objects, but it's on the interface. But we don't want to write these to file when we regenerate the FileBacked HMD's file.
        public object HlaInfoToSerialise => SmallGGroups;

        /// <remarks>
        /// ********************* WARNING! *********************
        /// The names of these parameters MUST stay in sync
        /// with the property names of the JSON objects found in
        /// the IntegrationTest MetadataDictionary test file.
        /// i.e. `all_hla_metadata.json`.
        /// ********************* WARNING! *********************
        /// </remarks>
        public FileBackedSmallGGroupsMetadata(
            Locus locus,                                   //*******************
            string lookupName,                              //****    See    ****
            List<string> smallGGroups,                      //****  warning  ****
            string serialisedHlaInfoType)                   //****   above   ****
        {                                                   //*******************
            Locus = locus;
            LookupName = lookupName;
            SmallGGroups = smallGGroups;
            SerialisedHlaInfoType = serialisedHlaInfoType;
        }
    }
}