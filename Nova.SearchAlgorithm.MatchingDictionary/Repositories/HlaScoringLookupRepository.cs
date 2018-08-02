using Microsoft.Extensions.Caching.Memory;
using Nova.SearchAlgorithm.Common.Repositories;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories
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
            IMemoryCache memoryCache)
            : base(factory, tableReferenceRepository, DataTableReferencePrefix, memoryCache, CacheKey)
        {
        }
    }
}