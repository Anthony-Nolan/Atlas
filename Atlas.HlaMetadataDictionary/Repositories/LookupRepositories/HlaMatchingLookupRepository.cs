using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Caching;
using Atlas.HlaMetadataDictionary.Exceptions;
using Atlas.HlaMetadataDictionary.Extensions;
using Atlas.HlaMetadataDictionary.Models.LookupEntities;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;

namespace Atlas.HlaMetadataDictionary.Repositories.LookupRepositories
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
            IPersistentCacheProvider cacheProvider)
            : base(factory, tableReferenceRepository, DataTableReferencePrefix, cacheProvider, CacheKey)
        {
        }

        public IEnumerable<string> GetAllPGroups(string hlaDatabaseVersion)
        {
            var versionedCacheKey = VersionedCacheKey(hlaDatabaseVersion);
            var metadataDictionary = cache.Get<Dictionary<string, HlaLookupTableEntity>>(versionedCacheKey);
            if (metadataDictionary != null)
            {
                return metadataDictionary.Values.SelectMany(v => v.ToHlaMatchingLookupResult()?.MatchingPGroups);
            }
            throw new MemoryCacheException($"{versionedCacheKey} table not cached!");
        }            
    }
}
