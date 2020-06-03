using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Caching;
using Atlas.HlaMetadataDictionary.Exceptions;
using Atlas.HlaMetadataDictionary.InternalModels.MetadataTableRows;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;

namespace Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories
{
    internal interface IHlaMatchingMetadataRepository : IHlaMetadataRepository
    {
        IEnumerable<string> GetAllPGroups(string hlaNomenclatureVersion);
    }

    internal class HlaMatchingMetadataRepository : HlaMetadataRepositoryBase, IHlaMatchingMetadataRepository
    {
        private const string DataTableReferencePrefix = "HlaMatchingLookupData";
        private const string CacheKey = "HlaMatchingLookup";

        public HlaMatchingMetadataRepository(
            ICloudTableFactory factory, 
            ITableReferenceRepository tableReferenceRepository,
            IPersistentCacheProvider cacheProvider)
            : base(factory, tableReferenceRepository, DataTableReferencePrefix, cacheProvider, CacheKey)
        {
        }

        public IEnumerable<string> GetAllPGroups(string hlaNomenclatureVersion)
        {
            var versionedCacheKey = VersionedCacheKey(hlaNomenclatureVersion);
            var metadataDictionary = cache.Get<Dictionary<string, HlaMetadataTableRow>>(versionedCacheKey);
            if (metadataDictionary != null)
            {
                return metadataDictionary.Values.SelectMany(v => v.ToHlaMatchingMetadata()?.MatchingPGroups);
            }
            throw new MemoryCacheException($"{versionedCacheKey} table not cached!");
        }            
    }
}
