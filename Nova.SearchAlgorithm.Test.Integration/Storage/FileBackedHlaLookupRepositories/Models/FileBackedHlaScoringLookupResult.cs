using Newtonsoft.Json;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using System;

namespace Nova.SearchAlgorithm.Test.Integration.Storage.FileBackedHlaLookupRepositories.Models
{
    public class FileBackedHlaScoringLookupResult : IHlaScoringLookupResult
    {
        public MatchLocus MatchLocus { get; }
        public string LookupName { get; }
        public TypingMethod TypingMethod { get; }
        public object HlaInfoToSerialise => HlaScoringInfo;
        public LookupNameCategory LookupNameCategory { get; }
        public IHlaScoringInfo HlaScoringInfo { get; }

        public FileBackedHlaScoringLookupResult(
            MatchLocus matchLocus, 
            string lookupName, 
            TypingMethod typingMethod, 
            LookupNameCategory lookupNameCategory,
            object hlaScoringInfo)
        {
            MatchLocus = matchLocus;
            LookupName = lookupName;
            TypingMethod = typingMethod;
            LookupNameCategory = lookupNameCategory;
            HlaScoringInfo = GetHlaScoringInfo(lookupNameCategory, hlaScoringInfo.ToString());
        }

        public HlaLookupTableEntity ConvertToTableEntity()
        {
            return new HlaLookupTableEntity(this)
            {
                LookupNameCategoryAsString = LookupNameCategory.ToString()
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
