using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary.ScoringLookup
{
    public class HlaScoringLookupResult : IHlaScoringLookupResult
    {
        public MatchLocus MatchLocus { get; }
        public string LookupName { get; }
        public TypingMethod TypingMethod { get; }
        public IPreCalculatedScoringInfo PreCalculatedHlaInfo { get; }       

        public HlaScoringLookupResult(
            MatchLocus matchLocus,
            string lookupName,
            TypingMethod typingMethod,
            IPreCalculatedScoringInfo preCalculatedHlaInfo)
        {
            MatchLocus = matchLocus;
            LookupName = lookupName;
            TypingMethod = typingMethod;
            PreCalculatedHlaInfo = preCalculatedHlaInfo;
        }

        public HlaLookupTableEntity ConvertToTableEntity()
        {
            return this.ToTableEntity();
        }        
    }
}
