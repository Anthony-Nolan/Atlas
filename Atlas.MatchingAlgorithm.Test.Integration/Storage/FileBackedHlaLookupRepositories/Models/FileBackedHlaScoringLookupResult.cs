using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.Models.LookupEntities;
using Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup;
using Newtonsoft.Json;

namespace Atlas.MatchingAlgorithm.Test.Integration.Storage.FileBackedHlaLookupRepositories.Models
{
    public class FileBackedHlaScoringLookupResult : IHlaScoringLookupResult
    {
        public Locus Locus { get; }
        public string LookupName { get; }
        public TypingMethod TypingMethod { get; }
        public object HlaInfoToSerialise => HlaScoringInfo;
        public IHlaScoringInfo HlaScoringInfo { get; }

        /// <remarks>
        /// ********************* WARNING! *********************
        /// The names of these parameters MUST stay in sync
        /// with the property names of the JSON objects found in
        /// the IntegrationTest MetadataDictionary test file.
        /// i.e. `all_hla_lookup_results.json`.
        /// ********************* WARNING! *********************
        /// </remarks>
        public FileBackedHlaScoringLookupResult(
            Locus locus,                            //*******************
            string lookupName,                      //****    See    ****
            TypingMethod typingMethod,              //****  warning  ****
            string hlaScoringInfoType,              //****   above   ****
            object hlaScoringInfo)                  //*******************
        {
            Locus = locus;
            LookupName = lookupName;
            TypingMethod = typingMethod;
            HlaScoringInfo = GetHlaScoringInfo(hlaScoringInfoType, hlaScoringInfo.ToString());
        }

        public HlaLookupTableEntity ConvertToTableEntity()
        {
            return new HlaLookupTableEntity(this);
        }

        public IEnumerable<IHlaScoringLookupResult> GetInTermsOfSingleAlleleScoringMetadata()
        {
            return HlaScoringInfo.ConvertToSingleAllelesInfo().Select(info => new HlaScoringLookupResult(
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
