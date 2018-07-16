using Nova.HLAService.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup
{
    public class HlaScoringLookupResult :
        IHlaScoringLookupResult,
        IStorableInCloudTable<HlaLookupTableEntity>
    {
        public MatchLocus MatchLocus { get; }
        public string LookupName { get; }
        public TypingMethod TypingMethod { get; }
        public HlaTypingCategory HlaTypingCategory { get; }
        public IPreCalculatedScoringInfo PreCalculatedHlaInfo { get; }

        public HlaScoringLookupResult(
            MatchLocus matchLocus,
            string lookupName,
            TypingMethod typingMethod,
            HlaTypingCategory hlaTypingCategory,
            IPreCalculatedScoringInfo preCalculatedHlaInfo)
        {
            MatchLocus = matchLocus;
            LookupName = lookupName;
            TypingMethod = typingMethod;
            HlaTypingCategory = hlaTypingCategory;
            PreCalculatedHlaInfo = preCalculatedHlaInfo;
        }

        public HlaLookupTableEntity ConvertToTableEntity()
        {
            return this.ToTableEntity();
        }
    }
}
