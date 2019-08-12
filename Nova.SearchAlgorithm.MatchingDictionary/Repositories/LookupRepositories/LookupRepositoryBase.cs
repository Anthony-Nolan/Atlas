using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.LookupRepositories
{
    /// <summary>
    /// Generic repository that persists data to a CloudTable
    /// & also caches it in memory for optimal read-access.
    /// </summary>
    /// <typeparam name="TStorable"></typeparam>
    /// <typeparam name="TTableEntity"></typeparam>
    public interface ILookupRepository<in TStorable, TTableEntity>
        where TTableEntity : TableEntity, new()
        where TStorable : IStorableInCloudTable<TTableEntity>
    {
        Task LoadDataIntoMemory(string hlaDatabaseVersion);
    }

    public abstract class LookupRepositoryBase<TStorable, TTableEntity> :
        ILookupRepository<TStorable, TTableEntity>
        where TTableEntity : TableEntity, new()
        where TStorable : IStorableInCloudTable<TTableEntity>
    {
        protected readonly IMemoryCache MemoryCache;

        private readonly ICloudTableFactory tableFactory;
        private readonly ITableReferenceRepository tableReferenceRepository;
        private readonly string functionalTableReferencePrefix;
        private readonly string cacheKey;
        private CloudTable cloudTable;

        protected LookupRepositoryBase(
            ICloudTableFactory factory,
            ITableReferenceRepository tableReferenceRepository,
            string functionalTableReferencePrefix,
            IMemoryCache memoryCache,
            string cacheKey)
        {
            tableFactory = factory;
            this.tableReferenceRepository = tableReferenceRepository;
            this.functionalTableReferencePrefix = functionalTableReferencePrefix;
            MemoryCache = memoryCache;
            this.cacheKey = cacheKey;
        }

        /// <summary>
        /// If you plan to use this repository with multiple async operations, this method should be called first
        /// </summary>
        public async Task LoadDataIntoMemory(string hlaDatabaseVersion)
        {
            var currentDataTable = await GetCurrentDataTable(hlaDatabaseVersion);
            var tableResults = new CloudTableBatchQueryAsync<TTableEntity>(currentDataTable);
            var dataToLoad = new Dictionary<string, TTableEntity>();

            while (tableResults.HasMoreResults)
            {
                var results = await tableResults.RequestNextAsync();
                foreach (var result in results)
                {
                    dataToLoad.Add(result.PartitionKey + result.RowKey, result);
                }
            }

            MemoryCache.Set(VersionedCacheKey(hlaDatabaseVersion), dataToLoad);
        }

        protected async Task RecreateDataTable(IEnumerable<TStorable> tableContents, IEnumerable<string> partitions, string hlaDatabaseVersion)
        {
            var tablePrefix = VersionedTableReferencePrefix(hlaDatabaseVersion);
            var newDataTable = await CreateNewDataTable(tablePrefix);
            await InsertIntoDataTable(tableContents, partitions, newDataTable);
            await tableReferenceRepository.UpdateTableReference(tablePrefix, newDataTable.Name);
            cloudTable = null;
        }

        protected async Task<TTableEntity> GetDataIfExists(string partition, string rowKey, string hlaDatabaseVersion)
        {
            var versionedCacheKey = VersionedCacheKey(hlaDatabaseVersion);
            
            if (MemoryCache.TryGetValue(versionedCacheKey, out Dictionary<string, TTableEntity> tableEntities))
            {
                return GetDataFromCache(partition, rowKey, tableEntities);
            }

            await LoadDataIntoMemory(hlaDatabaseVersion);
            if (MemoryCache.TryGetValue(versionedCacheKey, out tableEntities))
            {
                return GetDataFromCache(partition, rowKey, tableEntities);
            }

            throw new MemoryCacheException($"Failed to load data into the {versionedCacheKey} cache");
        }

        protected string VersionedCacheKey(string hlaDatabaseVersion)
        {
            return $"{cacheKey}:{hlaDatabaseVersion}";
        }

        private string VersionedTableReferencePrefix(string hlaDatabaseVersion)
        {
            return $"{functionalTableReferencePrefix}{hlaDatabaseVersion}";
        }

        /// <summary>
        /// The connection to the current data table is cached so we don't open unnecessary connections
        /// </summary>
        private async Task<CloudTable> GetCurrentDataTable(string hlaDatabaseVersion)
        {
            if (cloudTable == null)
            {
                var dataTableReference = await tableReferenceRepository.GetCurrentTableReference(VersionedTableReferencePrefix(hlaDatabaseVersion));
                cloudTable = await tableFactory.GetTable(dataTableReference);
            }
            return cloudTable;
        }

        private static TTableEntity GetDataFromCache(
            string partition, string rowKey, IReadOnlyDictionary<string, TTableEntity> matchingDictionary)
        {
            matchingDictionary.TryGetValue(partition + rowKey, out var tableEntity);
            return tableEntity;
        }

        private async Task<CloudTable> CreateNewDataTable(string tablePrefix)
        {
            var dataTableReference = tableReferenceRepository.GetNewTableReference(tablePrefix);
            return await tableFactory.GetTable(dataTableReference);
        }

        private static async Task InsertIntoDataTable(IEnumerable<TStorable> contents, IEnumerable<string> partitions, CloudTable dataTable)
        {
            var entities = contents
                .Select(data => data.ConvertToTableEntity())
                .ToList();

            foreach (var partition in partitions)
            {
                var partitionedEntities = entities
                    .Where(entity => entity.PartitionKey.Equals(partition));
                
                await dataTable.BatchInsert(partitionedEntities);
            }
        }
    }
}
