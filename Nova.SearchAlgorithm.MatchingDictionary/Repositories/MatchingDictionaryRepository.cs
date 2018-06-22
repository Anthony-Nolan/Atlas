using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.HlaTypingInfo;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories
{
    public interface IMatchingDictionaryRepository
    {
        Task RecreateMatchingDictionaryTable(IEnumerable<MatchingDictionaryEntry> dictionaryContents);
        Task<MatchingDictionaryEntry> GetMatchingDictionaryEntryIfExists(MatchLocus matchLocus, string lookupName, TypingMethod typingMethod);
        Task ConnectToCloudTable();
    }

    public class MatchingDictionaryRepository : IMatchingDictionaryRepository
    {
        private const string CacheKeyMatchingDictionary = "MatchingDictionary";
        
        private readonly ICloudTableFactory tableFactory;
        private readonly ITableReferenceRepository tableReferenceRepository;
        private readonly IMemoryCache memoryCache;
        private CloudTable cloudTable;

        public MatchingDictionaryRepository(ICloudTableFactory factory, ITableReferenceRepository tableReferenceRepository, IMemoryCache memoryCache)
        {
            tableFactory = factory;
            this.tableReferenceRepository = tableReferenceRepository;
            this.memoryCache = memoryCache;
        }

        public async Task RecreateMatchingDictionaryTable(IEnumerable<MatchingDictionaryEntry> dictionaryContents)
        {
            var newDataTable = CreateNewDataTable();
            InsertMatchingDictionaryEntriesIntoDataTable(dictionaryContents, newDataTable);
            await tableReferenceRepository.UpdateMatchingDictionaryTableReference(newDataTable.Name);
            cloudTable = null;
        }

        public async Task<MatchingDictionaryEntry> GetMatchingDictionaryEntryIfExists(MatchLocus matchLocus, string lookupName, TypingMethod typingMethod)
        {
            if (memoryCache.TryGetValue(CacheKeyMatchingDictionary,
                out Dictionary<string, MatchingDictionaryTableEntity> matchingDictionary))
            {
                var matchingDictionaryEntryFromCache = GetMatchingDictionaryEntryFromCache(matchLocus, lookupName, typingMethod, matchingDictionary);
                return matchingDictionaryEntryFromCache;
            }
            
            await LoadMatchingDictionaryIntoMemory();
            if (memoryCache.TryGetValue(CacheKeyMatchingDictionary, out matchingDictionary))
            {
                return GetMatchingDictionaryEntryFromCache(matchLocus, lookupName, typingMethod, matchingDictionary);
            }

            throw new MatchingDictionaryException("Failed to load matching dictionary into cache");
        }

        private static MatchingDictionaryEntry GetMatchingDictionaryEntryFromCache(MatchLocus matchLocus, string lookupName,
            TypingMethod typingMethod, IReadOnlyDictionary<string, MatchingDictionaryTableEntity> matchingDictionary)
        {
            var partition = MatchingDictionaryTableEntity.GetPartition(matchLocus);
            var rowKey = MatchingDictionaryTableEntity.GetRowKey(lookupName, typingMethod);

            matchingDictionary.TryGetValue(partition + rowKey, out var tableEntity);
            return tableEntity?.ToMatchingDictionaryEntry();
        }

        /// <summary>
        /// The connection to the current data table is cached so we don't open unnecessary connections
        /// As the lookups in the HLA Refresh job are asynchronous, we need a way of populating this cache synchronously up front
        /// If you plan to use this repository with multiple async operations, this method should be called first
        /// </summary>
        public async Task ConnectToCloudTable()
        {
            await GetCurrentDataTable();
            await LoadMatchingDictionaryIntoMemory();
        }
        
        private async Task LoadMatchingDictionaryIntoMemory()
        {
            var currentDataTable = await GetCurrentDataTable();

            var tableResults = new CloudTableBatchQueryAsync(new TableQuery<MatchingDictionaryTableEntity>(), currentDataTable);

            var matchingDictionary = new Dictionary<string, MatchingDictionaryTableEntity>();
            
            while (tableResults.HasMoreResults)
            {
                var results = await tableResults.RequestNextAsync();
                foreach (var result in results)
                {
                    matchingDictionary.Add(result.PartitionKey + result.RowKey, result);
                }
            }

            memoryCache.Set(CacheKeyMatchingDictionary, matchingDictionary);
        }

        private CloudTable CreateNewDataTable()
        {
            var dataTableReference = tableReferenceRepository.GetNewMatchingDictionaryTableReference();
            return tableFactory.GetOrCreateTable(dataTableReference);
        }

        private async Task<CloudTable> GetCurrentDataTable()
        {
            if (cloudTable == null)
            {
                var dataTableReference = await tableReferenceRepository.GetCurrentMatchingDictionaryTableReference();
                cloudTable = tableFactory.GetOrCreateTable(dataTableReference);
            }
            return cloudTable;
        }

        private static void InsertMatchingDictionaryEntriesIntoDataTable(IEnumerable<MatchingDictionaryEntry> contents, CloudTable dataTable)
        {
            var contentsList = contents.ToList();

            foreach (var partition in PermittedLocusNames.GetPermittedMatchLoci())
            {
                var partitionEntities = contentsList
                    .Where(entry => entry.MatchLocus.Equals(partition))
                    .Select(entry => entry.ToTableEntity());

                dataTable.BatchInsert(partitionEntities);
            }
        }        
    }

    internal class CloudTableBatchQueryAsync
    {
        private readonly TableQuery<MatchingDictionaryTableEntity> query;
        private readonly CloudTable table;

        private TableContinuationToken continuationToken = null;

        public CloudTableBatchQueryAsync(TableQuery<MatchingDictionaryTableEntity> query, CloudTable table)
        {
            this.query = query;
            this.table = table;
        }
        
        public bool HasMoreResults { get; private set; } = true;

        public async Task<IEnumerable<MatchingDictionaryTableEntity>> RequestNextAsync()
        {
            if (!HasMoreResults)
            {
                throw new Exception("More matching dictionary results were requested even though no more results are available. Check HasMoreResults before calling RequestNextAsync.");
            }

            TableQuerySegment<MatchingDictionaryTableEntity> tableQueryResult =
                await table.ExecuteQuerySegmentedAsync(query, continuationToken);

            continuationToken = tableQueryResult.ContinuationToken;

            if (continuationToken == null)
            {
                HasMoreResults = false;
            }

            return tableQueryResult.Results;
        }
    }

}
