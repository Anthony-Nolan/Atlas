using Atlas.Common.Caching;
using Atlas.HlaMetadataDictionary.Exceptions;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;
using LazyCache;
using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.HlaMetadataDictionary.Models.LookupEntities;
using Atlas.HlaMetadataDictionary.Models.Lookups;

namespace Atlas.HlaMetadataDictionary.Repositories.LookupRepositories
{
    /// <summary>
    /// Generic repository that persists data to a CloudTable
    /// & also caches it in memory for optimal read-access.
    /// </summary>
    internal interface ILookupRepository
    {
        Task LoadDataIntoMemory(string hlaDatabaseVersion);
    }

    internal abstract class LookupRepositoryBase<TStorable, TTableEntity> :
        ILookupRepository
        where TTableEntity : TableEntity, new()
        where TStorable : IHlaLookupResult
    {
        protected readonly IAppCache cache;

        private readonly ICloudTableFactory tableFactory;
        private readonly ITableReferenceRepository tableReferenceRepository;
        private readonly string functionalTableReferencePrefix;
        private readonly string cacheKey;
        private CloudTable cloudTable;

        protected LookupRepositoryBase(
            ICloudTableFactory factory,
            ITableReferenceRepository tableReferenceRepository,
            string functionalTableReferencePrefix,
            IPersistentCacheProvider cacheProvider,
            string cacheKey)
        {
            tableFactory = factory;
            this.tableReferenceRepository = tableReferenceRepository;
            this.functionalTableReferencePrefix = functionalTableReferencePrefix;
            this.cache = cacheProvider.Cache;
            this.cacheKey = cacheKey;
        }

        /// <summary>
        /// If you plan to use this repository with multiple async operations, this method should be called first
        /// </summary>
        public async Task LoadDataIntoMemory(string hlaDatabaseVersion)
        {
            var data = await FetchTableData(hlaDatabaseVersion);
            cache.Add(VersionedCacheKey(hlaDatabaseVersion), data);
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

            var tableData = await cache.GetOrAddAsync(versionedCacheKey, () => FetchTableData(hlaDatabaseVersion));

            if (tableData == null)
            {
                throw new MemoryCacheException($"Data: {partition}, {rowKey}: was not found in the {versionedCacheKey} cache");
            }
            
            return GetDataFromCache(partition, rowKey, tableData);
        }

        protected string VersionedCacheKey(string hlaDatabaseVersion)
        {
            return $"{cacheKey}:{hlaDatabaseVersion}";
        }

        private async Task<Dictionary<string, TTableEntity>> FetchTableData(string hlaDatabaseVersion)
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

            return dataToLoad;
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
            string partition, string rowKey, IReadOnlyDictionary<string, TTableEntity> metadataDictionary)
        {
            metadataDictionary.TryGetValue(partition + rowKey, out var tableEntity);
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
                .Select(data => new HlaLookupTableEntity(data))
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
