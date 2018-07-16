using Nova.HLAService.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary.ScoringLookup
{
    public class HlaScoringLookupResult :
        IHlaScoringLookupResult,
        IStorableInCloudTable<HlaLookupTableEntity>
    {
        public MatchLocus MatchLocus { get; }
        public string LookupName { get; }
        public TypingMethod TypingMethod => HlaTypingCategory == HlaTypingCategory.Serology ? TypingMethod.Serology : TypingMethod.Molecular;
        public HlaTypingCategory HlaTypingCategory { get; }
        public IPreCalculatedScoringInfo PreCalculatedHlaInfo { get; }

        public HlaScoringLookupResult(
            MatchLocus matchLocus,
            string lookupName,
            HlaTypingCategory hlaTypingCategory,
            IPreCalculatedScoringInfo preCalculatedHlaInfo)
        {
            MatchLocus = matchLocus;
            LookupName = lookupName;
            HlaTypingCategory = hlaTypingCategory;
            PreCalculatedHlaInfo = preCalculatedHlaInfo;
        }

        public HlaLookupTableEntity ConvertToTableEntity()
        {
            return this.ToTableEntity();
        }
    }
}
