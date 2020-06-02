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
        private const string CacheKey = "Dpb1TceGroupsLookup";

        public Dpb1TceGroupsMetadataRepository(
            ICloudTableFactory factory,
            ITableReferenceRepository tableReferenceRepository,
            IPersistentCacheProvider cacheProvider)
            : base(factory, tableReferenceRepository, DataTableReferencePrefix, cacheProvider, CacheKey)
        {
        }
    }
}