using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.MatchingLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.MatchingDictionary.Lookups
{
    internal class SerologyLookup : HlaMatchingLookupBase
    {
        public SerologyLookup(IHlaMatchingLookupRepository hlaMatchingLookupRepository) : base(hlaMatchingLookupRepository)
        {
        }

        public override Task<HlaMatchingLookupResult> PerformLookupAsync(MatchLocus matchLocus, string lookupName)
        {
            return GetHlaMatchingLookupResultIfExists(matchLocus, lookupName, TypingMethod.Serology);
        }
    }
}
