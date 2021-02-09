using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;

namespace Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories
{
    /// <summary>
    /// <see cref="ISmallGGroupToPGroupMetadataRepository.GetAllSmallGGroups"/> for ability to fetch all small g groups - while that
    /// functionality might be expected to be found here, this repository's data is structured based on efficient "hlaName:g-group" lookups,
    /// and using the "g-group:p-group" lookup repository is a much more efficient way of enumerating all small g groups.
    /// </summary>
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