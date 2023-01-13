using System.Collections.Generic;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Newtonsoft.Json;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs.Models
{
    public class FileBackedHlaMatchingMetadata : IHlaMatchingMetadata
    {
        public Locus Locus { get; }
        public string LookupName { get; }
        public TypingMethod TypingMethod { get; }
        public string SerialisedHlaInfoType { get; }
        public IList<string> MatchingPGroups { get; }
        public bool IsNullExpressingTyping { get; }
        [JsonIgnore] // This property isn't actually used anywhere for the FileBacked objects, but it's on the interface. But we don't want to write these to file when we regenerate the FileBacked HMD's file.
        public object HlaInfoToSerialise => MatchingPGroups;

        /// <remarks>
        /// ********************* WARNING! *********************
        /// The names of these parameters MUST stay in sync
        /// with the property names of the JSON objects found in
        /// the IntegrationTest MetadataDictionary test file.
        /// i.e. `all_hla_metadata.json`.
        /// ********************* WARNING! *********************
        /// </remarks>
        public FileBackedHlaMatchingMetadata(
            Locus locus,                             //*******************
            string lookupName,                       //****    See    ****
            TypingMethod typingMethod,               //****  warning  ****
            List<string> matchingPGroups,            //****   above   ****
            bool isNullExpressingTyping,             //*******************
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
