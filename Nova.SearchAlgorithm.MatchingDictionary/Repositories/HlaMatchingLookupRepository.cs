using Microsoft.Extensions.Caching.Memory;
using Nova.SearchAlgorithm.Common.Exceptions;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.MatchingLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories
{
    public interface IHlaMatchingLookupRepository : IHlaLookupRepository
    {
        Task<HlaMatchingLookupResult> GetHlaMatchLookupResultIfExists(MatchLocus matchLocus, string lookupName, TypingMethod typingMethod);
        IEnumerable<string> GetAllPGroups();
    }

    public class HlaMatchingLookupRepository : HlaLookupRepositoryBase, IHlaMatchingLookupRepository
    {
        private const string DataTableReferencePrefix = "HlaMatchingLookupData";
        private const string CacheKey = "HlaMatchingLookup";

        public HlaMatchingLookupRepository(
            ICloudTableFactory factory, 
            ITableReferenceRepository tableReferenceRepository,
            IMemoryCache memoryCache)
            : base(factory, tableReferenceRepository, DataTableReferencePrefix, memoryCache, CacheKey)
        {
        }

        public async Task<HlaMatchingLookupResult> GetHlaMatchLookupResultIfExists(MatchLocus matchLocus, string lookupName, TypingMethod typingMethod)
        {
            var entity = await GetHlaLookupTableEntityIfExists(matchLocus, lookupName, typingMethod);

            return entity?.ToHlaMatchingLookupResult();
        }

        public IEnumerable<string> GetAllPGroups()
        {
            if (MemoryCache.TryGetValue(CacheKey, out Dictionary<string, HlaLookupTableEntity> matchingDictionary))
            {
                return matchingDictionary.Values.SelectMany(v => v.ToHlaMatchingLookupResult()?.MatchingPGroups);
            }
            throw new MemoryCacheException($"{CacheKey} table not cached!");
        }            
    }
}
