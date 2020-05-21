using System;
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
            HlaTypingCategory hlaTypingCategory,    //****   above   ****
            object hlaScoringInfo)                  //*******************
        {
            Locus = locus;
            LookupName = lookupName;
            TypingMethod = typingMethod;
            HlaScoringInfo = GetHlaScoringInfo(hlaTypingCategory, hlaScoringInfo.ToString());
        }

        public HlaLookupTableEntity ConvertToTableEntity()
        {
            return new HlaLookupTableEntity(this);
        }

        private static IHlaScoringInfo GetHlaScoringInfo(
            HlaTypingCategory hlaScoringInfoType,
            string hlaScoringInfoString)
        {
            switch (hlaScoringInfoType)
            {
                case HlaTypingCategory.Serology:
                    return JsonConvert.DeserializeObject<SerologyScoringInfo>(hlaScoringInfoString);
                case HlaTypingCategory.Allele:
                    return JsonConvert.DeserializeObject<SingleAlleleScoringInfo>(hlaScoringInfoString);
                case HlaTypingCategory.NmdpCode:
                    return JsonConvert.DeserializeObject<MultipleAlleleScoringInfo>(hlaScoringInfoString);
                case HlaTypingCategory.XxCode:
                    return JsonConvert.DeserializeObject<ConsolidatedMolecularScoringInfo>(hlaScoringInfoString);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
