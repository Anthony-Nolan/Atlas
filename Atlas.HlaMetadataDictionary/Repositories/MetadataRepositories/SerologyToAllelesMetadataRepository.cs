using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;

namespace Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories
{
    internal interface ISerologyToAllelesMetadataRepository : IHlaMetadataRepository
    {
    }

    internal class SerologyToAllelesMetadataRepository : HlaMetadataRepositoryBase, ISerologyToAllelesMetadataRepository
    {
        private const string DataTableReferencePrefix = "SerologyToAllelesLookupData";
        private const string CacheKey = nameof(SerologyToAllelesMetadataRepository);

        public SerologyToAllelesMetadataRepository(
            ICloudTableFactory factory,
            ITableReferenceRepository tableReferenceRepository,
            IPersistentCacheProvider cacheProvider,
            ILogger logger)
            : base(factory, tableReferenceRepository, DataTableReferencePrefix, cacheProvider, CacheKey, logger)
        {
        }
    }
}