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
        public LookupNameCategory LookupNameCategory { get; }
        public IHlaScoringInfo HlaScoringInfo { get; }

        public FileBackedHlaScoringLookupResult(
            Locus locus, 
            string lookupName, 
            TypingMethod typingMethod, 
            LookupNameCategory lookupNameCategory,
            object hlaScoringInfo)
        {
            Locus = locus;
            LookupName = lookupName;
            TypingMethod = typingMethod;
            LookupNameCategory = lookupNameCategory;
            HlaScoringInfo = GetHlaScoringInfo(lookupNameCategory, hlaScoringInfo.ToString());
        }

        public HlaLookupTableEntity ConvertToTableEntity()
        {
            return new HlaLookupTableEntity(this)
            {
                LookupNameCategoryAsString = LookupNameCategory.ToString()  //QQ needs attention for rename
            };
        }

        private static IHlaScoringInfo GetHlaScoringInfo(
            LookupNameCategory lookupNameCategory,
            string hlaScoringInfoString)
        {
            switch (lookupNameCategory)
            {
                case LookupNameCategory.Serology:
                    return JsonConvert.DeserializeObject<SerologyScoringInfo>(hlaScoringInfoString);
                case LookupNameCategory.OriginalAllele:
                    return JsonConvert.DeserializeObject<SingleAlleleScoringInfo>(hlaScoringInfoString);
                case LookupNameCategory.NmdpCodeAllele:
                    return JsonConvert.DeserializeObject<MultipleAlleleScoringInfo>(hlaScoringInfoString);
                case LookupNameCategory.XxCode:
                    return JsonConvert.DeserializeObject<ConsolidatedMolecularScoringInfo>(hlaScoringInfoString);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
