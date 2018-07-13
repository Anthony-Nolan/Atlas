using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary.ScoringLookup
{
    public class HlaScoringLookupResult : 
        IHlaScoringLookupResult,
        IStorableInCloudTable<HlaLookupTableEntity>
    {
        public MatchLocus MatchLocus { get; set; }
        public string LookupName { get; set; }
        public TypingMethod TypingMethod => TypingMethod.Molecular;
        public IPreCalculatedScoringInfo PreCalculatedHlaInfo { get; set; }       

        public HlaScoringLookupResult(
            MatchLocus matchLocus,
            string lookupName,
            IPreCalculatedScoringInfo preCalculatedHlaInfo)
        {
            MatchLocus = matchLocus;
            LookupName = lookupName;
            PreCalculatedHlaInfo = preCalculatedHlaInfo;
        }

        public HlaLookupTableEntity ConvertToTableEntity()
        {
            return this.ToTableEntity();
        }        
    }
}
