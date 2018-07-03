using Microsoft.Extensions.Caching.Memory;
using Nova.SearchAlgorithm.Common.Exceptions;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.HlaTypingInfo;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories
{
    public interface IMatchingDictionaryRepository
    {
        Task RecreateMatchingDictionaryTable(IEnumerable<MatchingDictionaryEntry> dictionaryContents);
        Task<MatchingDictionaryEntry> GetMatchingDictionaryEntryIfExists(MatchLocus matchLocus, string lookupName, TypingMethod typingMethod);
        Task LoadMatchingDictionaryIntoMemory();
        IEnumerable<string> GetAllPGroups();
    }

    public class MatchingDictionaryRepository : 
        LookupRepositoryBase<MatchingDictionaryEntry, MatchingDictionaryTableEntity>,
        IMatchingDictionaryRepository
    {
        private const string DataTableReferencePrefix = "MatchingDictionaryData";
        private const string CacheKeyMatchingDictionary = "MatchingDictionary";

        public MatchingDictionaryRepository(
            ICloudTableFactory factory, 
            ITableReferenceRepository tableReferenceRepository,
            IMemoryCache memoryCache)
            : base(factory, tableReferenceRepository, DataTableReferencePrefix, memoryCache, CacheKeyMatchingDictionary)
        {
        }

        public async Task RecreateMatchingDictionaryTable(IEnumerable<MatchingDictionaryEntry> dictionaryContents)
        {
            var partitions = GetTablePartitions();
            await RecreateDataTable(dictionaryContents, partitions);
        }

        public async Task<MatchingDictionaryEntry> GetMatchingDictionaryEntryIfExists(MatchLocus matchLocus, string lookupName, TypingMethod typingMethod)
        {
            var partition = MatchingDictionaryTableEntity.GetPartition(matchLocus);
            var rowKey = MatchingDictionaryTableEntity.GetRowKey(lookupName, typingMethod);
            var entity = await GetDataIfExists(partition, rowKey);

            return entity?.ToMatchingDictionaryEntry();
        }

        /// <summary>
        /// The connection to the current data table is cached so we don't open unnecessary connections
        /// If you plan to use this repository with multiple async operations, this method should be called first
        /// </summary>
        public async Task LoadMatchingDictionaryIntoMemory()
        {
            await LoadDataIntoMemory();
        }

        public IEnumerable<string> GetAllPGroups()
        {
            if (MemoryCache.TryGetValue(CacheKeyMatchingDictionary, out Dictionary<string, MatchingDictionaryTableEntity> matchingDictionary))
            {
                return matchingDictionary.Values.SelectMany(v => v.ToMatchingDictionaryEntry().MatchingPGroups);
            }
            throw new MemoryCacheException("Matching Dictionary not cached!");
        }

        protected override IEnumerable<string> GetTablePartitions()
        {
            return PermittedLocusNames
                .GetPermittedMatchLoci()
                .Select(matchLocus => matchLocus.ToString());
        }              
    }
}
