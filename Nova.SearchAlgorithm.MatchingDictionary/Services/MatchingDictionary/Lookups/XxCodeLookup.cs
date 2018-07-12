using System.Threading.Tasks;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.MatchingDictionary.Lookups
{
    internal class XxCodeLookup : HlaTypingLookupBase
    {
        public XxCodeLookup(IPreCalculatedHlaMatchRepository preCalculatedHlaMatchRepository) : base(preCalculatedHlaMatchRepository)
        {
        }

        public override Task<PreCalculatedHlaMatchInfo> PerformLookupAsync(MatchLocus matchLocus, string lookupName)
        {
            var firstField = lookupName.Split(':')[0];
            return GetPreCalculatedHlaMatchInfoIfExists(matchLocus, firstField, TypingMethod.Molecular);
        }
    }
}
