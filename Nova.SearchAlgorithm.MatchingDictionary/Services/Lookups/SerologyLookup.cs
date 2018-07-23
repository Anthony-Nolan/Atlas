using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Lookups
{
    internal class SerologyLookup : HlaLookupBase
    {
        public SerologyLookup(IHlaLookupRepository hlaLookupRepository) : base(hlaLookupRepository)
        {
        }

        public override async Task<IEnumerable<HlaLookupTableEntity>> PerformLookupAsync(MatchLocus matchLocus, string lookupName)
        {
            var entity = await GetHlaLookupTableEntityIfExists(matchLocus, lookupName, TypingMethod.Serology);
            return new List<HlaLookupTableEntity> { entity };
        }
    }
}
