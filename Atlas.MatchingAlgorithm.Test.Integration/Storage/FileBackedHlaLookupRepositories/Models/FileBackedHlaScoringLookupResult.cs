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
        public HlaTypingCategory HlaTypingCategory { get; }
        public IHlaScoringInfo HlaScoringInfo { get; }

        public FileBackedHlaScoringLookupResult(
            Locus locus, 
            string lookupName, 
            TypingMethod typingMethod, 
            HlaTypingCategory hlaTypingCategory,
            object hlaScoringInfo)
        {
            Locus = locus;
            LookupName = lookupName;
            TypingMethod = typingMethod;
            HlaTypingCategory = hlaTypingCategory;
            HlaScoringInfo = GetHlaScoringInfo(hlaTypingCategory, hlaScoringInfo.ToString());
        }

        public HlaLookupTableEntity ConvertToTableEntity()
        {
            return new HlaLookupTableEntity(this)
            {
                LookupNameCategoryAsString = HlaTypingCategory.ToString() //QQ needs attention for rename
            };
        }

        private static IHlaScoringInfo GetHlaScoringInfo(
            HlaTypingCategory hlaTypingCategory,
            string hlaScoringInfoString)
        {
            switch (hlaTypingCategory)
            {
                case HlaTypingCategory.Serology:
                    return JsonConvert.DeserializeObject<SerologyScoringInfo>(hlaScoringInfoString);
                case HlaTypingCategory.OriginalAllele:
                    return JsonConvert.DeserializeObject<SingleAlleleScoringInfo>(hlaScoringInfoString);
                case HlaTypingCategory.NmdpCodeAllele:
                    return JsonConvert.DeserializeObject<MultipleAlleleScoringInfo>(hlaScoringInfoString);
                case HlaTypingCategory.XxCode:
                    return JsonConvert.DeserializeObject<ConsolidatedMolecularScoringInfo>(hlaScoringInfoString);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
