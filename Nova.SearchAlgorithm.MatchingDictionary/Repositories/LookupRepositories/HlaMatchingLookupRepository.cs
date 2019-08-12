using Microsoft.Extensions.Caching.Memory;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories
{
    public interface IHlaMatchingLookupRepository : IHlaLookupRepository
    {
        IEnumerable<string> GetAllPGroups(string hlaDatabaseVersion);
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

        public IEnumerable<string> GetAllPGroups(string hlaDatabaseVersion)
        {
            var versionedCacheKey = VersionedCacheKey(hlaDatabaseVersion);
            if (MemoryCache.TryGetValue(versionedCacheKey, out Dictionary<string, HlaLookupTableEntity> matchingDictionary))
            {
                return matchingDictionary.Values.SelectMany(v => v.ToHlaMatchingLookupResult()?.MatchingPGroups);
            }
            throw new MemoryCacheException($"{versionedCacheKey} table not cached!");
        }            
    }
}
