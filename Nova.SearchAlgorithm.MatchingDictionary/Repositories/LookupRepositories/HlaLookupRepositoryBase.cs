using Microsoft.Extensions.Caching.Memory;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.LookupRepositories;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories
{
    public interface IHlaLookupRepository : ILookupRepository<IHlaLookupResult, HlaLookupTableEntity>
    {
        Task RecreateHlaLookupTable(IEnumerable<IHlaLookupResult> lookupResults);
        Task<HlaLookupTableEntity> GetHlaLookupTableEntityIfExists(Locus locus, string lookupName, TypingMethod typingMethod);
    }

    public abstract class HlaLookupRepositoryBase :
        LookupRepositoryBase<IHlaLookupResult, HlaLookupTableEntity>,
        IHlaLookupRepository
    {
        protected HlaLookupRepositoryBase(
            ICloudTableFactory factory, 
            ITableReferenceRepository tableReferenceRepository,
            string dataTableReferencePrefix,
            IMemoryCache memoryCache,
            string cacheKey)
            : base(factory, tableReferenceRepository, dataTableReferencePrefix, memoryCache, cacheKey)
        {
        }

        public async Task RecreateHlaLookupTable(IEnumerable<IHlaLookupResult> lookupResults)
        {
            await RecreateDataTable(lookupResults, HlaLookupTableKeyManager.GetTablePartitionKeys());
        }

        public async Task<HlaLookupTableEntity> GetHlaLookupTableEntityIfExists(Locus locus, string lookupName, TypingMethod typingMethod)
        {
            var partition = HlaLookupTableKeyManager.GetEntityPartitionKey(locus);
            var rowKey = HlaLookupTableKeyManager.GetEntityRowKey(lookupName, typingMethod);

            return await GetDataIfExists(partition, rowKey);
        }
    }
}
