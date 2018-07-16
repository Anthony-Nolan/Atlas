using Nova.HLAService.Client.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup
{
    public class HlaScoringLookupResult : IHlaScoringLookupResult
    {
        public MatchLocus MatchLocus { get; }
        public string LookupName { get; }
        public TypingMethod TypingMethod => HlaTypingCategory == HlaTypingCategory.Serology ? TypingMethod.Serology : TypingMethod.Molecular;
        public HlaTypingCategory HlaTypingCategory { get; }
        public IHlaScoringInfo HlaScoringInfo { get; }

        public HlaScoringLookupResult(
            MatchLocus matchLocus,
            string lookupName,
            HlaTypingCategory hlaTypingCategory,
            IHlaScoringInfo hlaScoringInfo)
        {
            MatchLocus = matchLocus;
            LookupName = lookupName;
            HlaTypingCategory = hlaTypingCategory;
            HlaScoringInfo = hlaScoringInfo;
        }

        public HlaLookupTableEntity ConvertToTableEntity()
        {
            return this.ToTableEntity();
        }
    }
}
