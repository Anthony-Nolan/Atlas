using System.Collections.Generic;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;
using Newtonsoft.Json;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs.Models
{
    public class FileBackedAlleleGroupMetadata : IAlleleGroupMetadata
    {
        public Locus Locus { get; }
        public string LookupName { get; }
        public TypingMethod TypingMethod { get; }
        public string SerialisedHlaInfoType { get; }
        public List<string> AllelesInGroup { get; }
        [JsonIgnore] // This property isn't actually used anywhere for the FileBacked objects, but it's on the interface. But we don't want to write these to file when we regenerate the FileBacked HMD's file.
        public object HlaInfoToSerialise => AllelesInGroup;

        /// <remarks>
        /// ********************* WARNING! *********************
        /// The names of these parameters MUST stay in sync
        /// with the property names of the JSON objects found in
        /// the IntegrationTest MetadataDictionary test file.
        /// i.e. `all_hla_metadata.json`.
        /// ********************* WARNING! *********************
        /// </remarks>
        public FileBackedAlleleGroupMetadata(
            Locus locus,                             //*******************
            string lookupName,                       //****    See    ****
            TypingMethod typingMethod,               //****  warning  ****
            string serialisedHlaInfoType,            //****   above   ****
            List<string> allelesInGroup)             //*******************
        {
            Locus = locus;
            LookupName = lookupName;
            TypingMethod = typingMethod;
            SerialisedHlaInfoType = serialisedHlaInfoType;
            AllelesInGroup = allelesInGroup;
        }
    }
}