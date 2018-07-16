using Microsoft.Extensions.Caching.Memory;
using Nova.SearchAlgorithm.Common.Exceptions;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.HlaTypingInfo;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary.MatchingLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories
{
    public interface IHlaMatchingLookupRepository
    {
        Task RecreateHlaMatchingLookupTable(IEnumerable<HlaMatchingLookupResult> dictionaryContents);
        Task<HlaMatchingLookupResult> GetHlaMatchLookupResultIfExists(MatchLocus matchLocus, string lookupName, TypingMethod typingMethod);
        Task LoadHlaMatchingLookupTableIntoMemory();
        IEnumerable<string> GetAllPGroups();
    }

    public class HlaMatchingLookupRepository : 
        LookupRepositoryBase<HlaMatchingLookupResult, HlaLookupTableEntity>,
        IHlaMatchingLookupRepository
    {
        private const string DataTableReferencePrefix = "HlaMatchingLookupData";
        private const string CacheKey = "HlaMatchingLookup";

        public HlaMatchingLookupRepository(
            ICloudTableFactory factory, 
            ITableReferenceRepository tableReferenceRepository,
            IMemoryCache memoryCache)
            : base(factory, tableReferenceRepository, DataTableReferencePrefix, memoryCache, CacheKey)
        {
        }

        public async Task RecreateHlaMatchingLookupTable(IEnumerable<HlaMatchingLookupResult> dictionaryContents)
        {
            var partitions = GetTablePartitions();
            await RecreateDataTable(dictionaryContents, partitions);
        }

        public async Task<HlaMatchingLookupResult> GetHlaMatchLookupResultIfExists(MatchLocus matchLocus, string lookupName, TypingMethod typingMethod)
        {
            var partition = HlaLookupTableEntity.GetPartition(matchLocus);
            var rowKey = HlaLookupTableEntity.GetRowKey(lookupName, typingMethod);
            var entity = await GetDataIfExists(partition, rowKey);

            return entity?.ToHlaMatchingLookupResult();
        }

        public async Task LoadHlaMatchingLookupTableIntoMemory()
        {
            await LoadDataIntoMemory();
        }

        public IEnumerable<string> GetAllPGroups()
        {
            if (MemoryCache.TryGetValue(CacheKey, out Dictionary<string, HlaLookupTableEntity> matchingDictionary))
            {
                return matchingDictionary.Values.SelectMany(v => v.ToHlaMatchingLookupResult()?.MatchingPGroups);
            }
            throw new MemoryCacheException($"{CacheKey} table not cached!");
        }

        protected override IEnumerable<string> GetTablePartitions()
        {
            return PermittedLocusNames
                .GetPermittedMatchLoci()
                .Select(matchLocus => matchLocus.ToString());
        }              
    }
}
