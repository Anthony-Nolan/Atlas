using LazyCache;
using Atlas.MatchingAlgorithm.MatchingDictionary.Repositories.AzureStorage;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Repositories
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
            IAppCache cache)
            : base(factory, tableReferenceRepository, DataTableReferencePrefix, cache, CacheKey)
        {
        }
    }
}