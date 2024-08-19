using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;

namespace Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories
{
    internal interface IDpb1TceGroupsMetadataRepository : IHlaMetadataRepository
    {
    }

    internal class Dpb1TceGroupsMetadataRepository : HlaMetadataRepositoryBase, IDpb1TceGroupsMetadataRepository
    {
        private const string DataTableReferencePrefix = "Dpb1TceGroupsLookupData";
        private const string CacheKey = nameof(Dpb1TceGroupsMetadataRepository);

        public Dpb1TceGroupsMetadataRepository(
            ITableClientFactory factory,
            ITableReferenceRepository tableReferenceRepository,
            IPersistentCacheProvider cacheProvider,
            ILogger logger)
            : base(factory, tableReferenceRepository, DataTableReferencePrefix, cacheProvider, CacheKey, logger)
        {
        }
    }
}