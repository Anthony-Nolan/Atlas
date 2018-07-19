using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Lookups
{
    internal class XxCodeLookup : HlaLookupBase
    {
        public XxCodeLookup(IHlaLookupRepository hlaLookupRepository) : 
            base(hlaLookupRepository)
        {
        }

        public override async Task<IEnumerable<HlaLookupTableEntity>> PerformLookupAsync(MatchLocus matchLocus, string lookupName)
        {
            var firstField = lookupName.Split(':')[0];
            var entity = await GetHlaLookupTableEntityIfExists(matchLocus, firstField, TypingMethod.Molecular);
            return new List<HlaLookupTableEntity> { entity };
        }
    }
}
