using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.HLATypings;
using Atlas.MatchingAlgorithm.MatchingDictionary.Repositories;
using Atlas.MatchingAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Services.Lookups
{
    internal class XxCodeLookup : HlaLookupBase
    {
        public XxCodeLookup(IHlaLookupRepository hlaLookupRepository) : 
            base(hlaLookupRepository)
        {
        }

        public override async Task<IEnumerable<HlaLookupTableEntity>> PerformLookupAsync(Locus locus, string lookupName, string hlaDatabaseVersion)
        {
            var entity = await GetHlaLookupTableEntityIfExists(locus, lookupName, TypingMethod.Molecular, hlaDatabaseVersion);
            return new List<HlaLookupTableEntity> { entity };
        }
    }
}
