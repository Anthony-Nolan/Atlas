using LazyCache;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;

namespace Atlas.HlaMetadataDictionary.Repositories
{
    public interface IDpb1TceGroupsLookupRepository : IHlaLookupRepository
    {
    }

    public class Dpb1TceGroupsLookupRepository : HlaLookupRepositoryBase, IDpb1TceGroupsLookupRepository
    {
        private const string DataTableReferencePrefix = "Dpb1TceGroupsLookupData";
        private const string CacheKey = "Dpb1TceGroupsLookup";

        public Dpb1TceGroupsLookupRepository(
            ICloudTableFactory factory,
            ITableReferenceRepository tableReferenceRepository,
            IAppCache cache)
            : base(factory, tableReferenceRepository, DataTableReferencePrefix, cache, CacheKey)
        {
        }
    }
}