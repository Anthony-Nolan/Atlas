using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.MatchingLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Lookups
{
    internal class XxCodeLookup : HlaMatchingLookupBase
    {
        public XxCodeLookup(IHlaMatchingLookupRepository hlaMatchingLookupRepository) : base(hlaMatchingLookupRepository)
        {
        }

        public override Task<HlaMatchingLookupResult> PerformLookupAsync(MatchLocus matchLocus, string lookupName)
        {
            var firstField = lookupName.Split(':')[0];
            return GetHlaMatchingLookupResultIfExists(matchLocus, firstField, TypingMethod.Molecular);
        }
    }
}
