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
    public interface IPreCalculatedHlaMatchRepository
    {
        Task RecreatePreCalculatedHlaMatchesTable(IEnumerable<PreCalculatedHlaMatchInfo> dictionaryContents);
        Task<PreCalculatedHlaMatchInfo> GetPreCalculatedHlaMatchInfoIfExists(MatchLocus matchLocus, string lookupName, TypingMethod typingMethod);
        Task LoadPreCalculatedHlaMatchesIntoMemory();
        IEnumerable<string> GetAllPGroups();
    }

    public class PreCalculatedHlaMatchRepository : 
        LookupRepositoryBase<PreCalculatedHlaMatchInfo, MatchingDictionaryTableEntity>,
        IPreCalculatedHlaMatchRepository
    {
        private const string DataTableReferencePrefix = "PreCalculatedHlaMatchInfoData";
        private const string CacheKeyMatchingDictionary = "PreCalculatedHlaMatchInfo";

        public PreCalculatedHlaMatchRepository(
            ICloudTableFactory factory, 
            ITableReferenceRepository tableReferenceRepository,
            IMemoryCache memoryCache)
            : base(factory, tableReferenceRepository, DataTableReferencePrefix, memoryCache, CacheKeyMatchingDictionary)
        {
        }

        public async Task RecreatePreCalculatedHlaMatchesTable(IEnumerable<PreCalculatedHlaMatchInfo> dictionaryContents)
        {
            var partitions = GetTablePartitions();
            await RecreateDataTable(dictionaryContents, partitions);
        }

        public async Task<PreCalculatedHlaMatchInfo> GetPreCalculatedHlaMatchInfoIfExists(MatchLocus matchLocus, string lookupName, TypingMethod typingMethod)
        {
            var partition = MatchingDictionaryTableEntity.GetPartition(matchLocus);
            var rowKey = MatchingDictionaryTableEntity.GetRowKey(lookupName, typingMethod);
            var entity = await GetDataIfExists(partition, rowKey);

            return entity?.ToPreCalculatedHlaMatchInfo();
        }

        public async Task LoadPreCalculatedHlaMatchesIntoMemory()
        {
            await LoadDataIntoMemory();
        }

        public IEnumerable<string> GetAllPGroups()
        {
            if (MemoryCache.TryGetValue(CacheKeyMatchingDictionary, out Dictionary<string, MatchingDictionaryTableEntity> matchingDictionary))
            {
                return matchingDictionary.Values.SelectMany(v => v.ToPreCalculatedHlaMatchInfo().MatchingPGroups);
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
