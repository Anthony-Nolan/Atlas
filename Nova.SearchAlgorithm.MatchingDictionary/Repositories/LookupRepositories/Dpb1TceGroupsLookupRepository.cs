using Microsoft.Extensions.Caching.Memory;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories
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
            IMemoryCache memoryCache)
            : base(factory, tableReferenceRepository, DataTableReferencePrefix, memoryCache, CacheKey)
        {
        }
    }
}