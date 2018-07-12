using System.Threading.Tasks;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.MatchingDictionary.Lookups
{
    internal class SerologyLookup : HlaTypingLookupBase
    {
        public SerologyLookup(IPreCalculatedHlaMatchRepository preCalculatedHlaMatchRepository) : base(preCalculatedHlaMatchRepository)
        {
        }

        public override Task<PreCalculatedHlaMatchInfo> PerformLookupAsync(MatchLocus matchLocus, string lookupName)
        {
            return GetPreCalculatedHlaMatchInfoIfExists(matchLocus, lookupName, TypingMethod.Serology);
        }
    }
}
