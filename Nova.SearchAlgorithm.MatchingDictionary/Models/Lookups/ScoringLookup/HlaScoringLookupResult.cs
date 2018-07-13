using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup
{
    public class HlaScoringLookupResult : IHlaScoringLookupResult
    {
        public MatchLocus MatchLocus { get; }
        public string LookupName { get; }
        public TypingMethod TypingMethod { get; }
        public IHlaScoringInfo HlaScoringInfo { get; }

        public HlaScoringLookupResult(
            MatchLocus matchLocus,
            string lookupName,
            TypingMethod typingMethod,
            IHlaScoringInfo hlaScoringInfo)
        {
            MatchLocus = matchLocus;
            LookupName = lookupName;
            TypingMethod = typingMethod;
            HlaScoringInfo = hlaScoringInfo;
        }

        public HlaLookupTableEntity ConvertToTableEntity()
        {
            return this.ToTableEntity();
        }
    }
}
