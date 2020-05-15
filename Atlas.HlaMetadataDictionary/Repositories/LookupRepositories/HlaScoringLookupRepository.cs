using Atlas.Common.Caching;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;

namespace Atlas.HlaMetadataDictionary.Repositories
{
    public interface IHlaScoringLookupRepository : IHlaLookupRepository
    {
    }

    public class HlaScoringLookupRepository : HlaLookupRepositoryBase, IHlaScoringLookupRepository
    {
        private const string DataTableReferencePrefix = "HlaScoringLookupData";
        private const string CacheKey = "HlaScoringLookup";

        public HlaScoringLookupRepository(
            ICloudTableFactory factory,
            ITableReferenceRepository tableReferenceRepository,
            IPersistentCacheProvider cacheProvider)
            : base(factory, tableReferenceRepository, DataTableReferencePrefix, cacheProvider, CacheKey)
        {
        }
    }
}