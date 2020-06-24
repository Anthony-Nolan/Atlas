using Atlas.Common.Caching;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;

namespace Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories
{
    internal interface IAlleleGroupsMetadataRepository : IHlaMetadataRepository
    {
    }

    internal class AlleleGroupsMetadataRepository : HlaMetadataRepositoryBase, IAlleleGroupsMetadataRepository
    {
        private const string DataTableReferencePrefix = "AlleleGroupsLookupData";
        private const string CacheKey = "AlleleGroupsLookup";

        public AlleleGroupsMetadataRepository(
            ICloudTableFactory factory,
            ITableReferenceRepository tableReferenceRepository,
            IPersistentCacheProvider cacheProvider)
            : base(factory, tableReferenceRepository, DataTableReferencePrefix, cacheProvider, CacheKey)
        {
        }
    }
}