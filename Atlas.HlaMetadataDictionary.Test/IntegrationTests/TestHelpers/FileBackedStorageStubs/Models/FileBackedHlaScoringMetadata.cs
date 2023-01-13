using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Newtonsoft.Json;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs.Models
{
    public class FileBackedHlaScoringMetadata : IHlaScoringMetadata
    {
        public Locus Locus { get; }
        public string LookupName { get; }
        public TypingMethod TypingMethod { get; }
        public string SerialisedHlaInfoType { get; }
        public IHlaScoringInfo HlaScoringInfo { get; }
        [JsonIgnore] // This property isn't actually used anywhere for the FileBacked objects, but it's on the interface. But we don't want to write these to file when we regenerate the FileBacked HMD's file.
        public object HlaInfoToSerialise => HlaScoringInfo;

        /// <remarks>
        /// ********************* WARNING! *********************
        /// The names of these parameters MUST stay in sync
        /// with the property names of the JSON objects found in
        /// the IntegrationTest MetadataDictionary test file.
        /// i.e. `all_hla_metadata.json`.
        /// ********************* WARNING! *********************
        /// </remarks>
        public FileBackedHlaScoringMetadata(
            Locus locus,                            //*******************
            string lookupName,                      //****    See    ****
            TypingMethod typingMethod,              //****  warning  ****
            string serialisedHlaInfoType,           //****   above   ****
            object hlaScoringInfo)                  //*******************
        {
            Locus = locus;
            LookupName = lookupName;
            TypingMethod = typingMethod;
            SerialisedHlaInfoType = serialisedHlaInfoType;
            HlaScoringInfo = GetHlaScoringInfo(serialisedHlaInfoType, hlaScoringInfo.ToString());
        }

        public IEnumerable<IHlaScoringMetadata> GetInTermsOfSingleAlleleScoringMetadata()
        {
            return HlaScoringInfo.ConvertToSingleAllelesInfo().Select(info => new HlaScoringMetadata(
                Locus,
                info.AlleleName,
                info,
                TypingMethod
            ));
        }

        private static IHlaScoringInfo GetHlaScoringInfo(
            string hlaScoringInfoType,
            string hlaScoringInfoString)
        {
            switch (hlaScoringInfoType)
            {
                case nameof(SerologyScoringInfo):
                    return JsonConvert.DeserializeObject<SerologyScoringInfo>(hlaScoringInfoString);
                case nameof(SingleAlleleScoringInfo):
                    return JsonConvert.DeserializeObject<SingleAlleleScoringInfo>(hlaScoringInfoString);
                case nameof(MultipleAlleleScoringInfo):
                    return JsonConvert.DeserializeObject<MultipleAlleleScoringInfo>(hlaScoringInfoString);
                case nameof(ConsolidatedMolecularScoringInfo):
                    return JsonConvert.DeserializeObject<ConsolidatedMolecularScoringInfo>(hlaScoringInfoString);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
