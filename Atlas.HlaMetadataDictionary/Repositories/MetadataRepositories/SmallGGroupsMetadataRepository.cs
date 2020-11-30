using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;

namespace Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories
{
    internal interface ISmallGGroupsMetadataRepository : IHlaMetadataRepository
    {
    }

    internal class SmallGGroupsMetadataRepository : HlaMetadataRepositoryBase, ISmallGGroupsMetadataRepository
    {
        private const string DataTableReferencePrefix = "SmallGGroupsLookupData";
        private const string CacheKey = nameof(SmallGGroupsMetadataRepository);

        public SmallGGroupsMetadataRepository(
            ICloudTableFactory factory,
            ITableReferenceRepository tableReferenceRepository,
            IPersistentCacheProvider cacheProvider,
            ILogger logger)
            : base(factory, tableReferenceRepository, DataTableReferencePrefix, cacheProvider, CacheKey, logger)
        {
        }
    }
}