using Microsoft.Extensions.Caching.Memory;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories
{
    public interface IHlaScoringLookupRepository : IHlaLookupRepository
    {
        Task<IHlaScoringLookupResult> GetHlaScoringLookupResultIfExists(
            MatchLocus matchLocus, string lookupName, TypingMethod typingMethod);
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

        public async Task<IHlaScoringLookupResult> GetHlaScoringLookupResultIfExists(
            MatchLocus matchLocus, string lookupName, TypingMethod typingMethod)
        {
            var entity = await GetHlaLookupTableEntityIfExists(matchLocus, lookupName, typingMethod);

            return entity?.ToHlaScoringLookupResult();
        }
    }
}