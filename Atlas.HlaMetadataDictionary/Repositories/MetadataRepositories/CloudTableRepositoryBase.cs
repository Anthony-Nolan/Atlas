using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Caching;
using Atlas.HlaMetadataDictionary.Exceptions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.InternalModels.MetadataTableRows;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;
using LazyCache;
using Microsoft.WindowsAzure.Storage.Table;

namespace Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories
{
    /// <summary>
    /// Generic repository that persists data to a CloudTable
    /// & also caches it in memory for optimal read-access.
    /// </summary>
    internal interface IWarmableRepository
    {
        Task LoadDataIntoMemory(string hlaNomenclatureVersion);
    }

    internal abstract class CloudTableRepositoryBase<TStorable, TTableRow> :
        IWarmableRepository
        where TTableRow : TableEntity, new()
        where TStorable : ISerialisableHlaMetadata
    {
        protected readonly IAppCache cache;

        private readonly ICloudTableFactory tableFactory;
        private readonly ITableReferenceRepository tableReferenceRepository;
        private readonly string functionalTableReferencePrefix;
        private readonly string cacheKey;
        private CloudTable cloudTable;

        protected CloudTableRepositoryBase(
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
        public async Task LoadDataIntoMemory(string hlaNomenclatureVersion)
        {
            var data = await FetchTableData(hlaNomenclatureVersion);
            cache.Add(VersionedCacheKey(hlaNomenclatureVersion), data);
        }

        protected async Task RecreateDataTable(IEnumerable<TStorable> tableContents, string hlaNomenclatureVersion)
        {
            var tablePrefix = VersionedTableReferencePrefix(hlaNomenclatureVersion);
            var newDataTable = await CreateNewDataTable(tablePrefix);
            await InsertIntoDataTable(tableContents, newDataTable);
            await tableReferenceRepository.UpdateTableReference(tablePrefix, newDataTable.Name);
            cloudTable = null;
        }

        protected async Task<TTableRow> GetDataRowIfExists(string partition, string rowKey, string hlaNomenclatureVersion)
        {
            var versionedCacheKey = VersionedCacheKey(hlaNomenclatureVersion);

            var tableData = await cache.GetOrAddAsync(versionedCacheKey, () => FetchTableData(hlaNomenclatureVersion));

            if (tableData == null)
            {
                throw new MemoryCacheException($"Data: {partition}, {rowKey}: was not found in the {versionedCacheKey} cache");
            }
            
            return GetDataFromCache(partition, rowKey, tableData);
        }

        protected string VersionedCacheKey(string hlaNomenclatureVersion)
        {
            return $"{cacheKey}:{hlaNomenclatureVersion}";
        }

        private async Task<Dictionary<string, TTableRow>> FetchTableData(string hlaNomenclatureVersion)
        {
            var currentDataTable = await GetCurrentDataTable(hlaNomenclatureVersion);
            var tableResults = new CloudTableBatchQueryAsync<TTableRow>(currentDataTable);
            var dataToLoad = new Dictionary<string, TTableRow>();

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

        private string VersionedTableReferencePrefix(string hlaNomenclatureVersion)
        {
            return $"{functionalTableReferencePrefix}{hlaNomenclatureVersion}";
        }

        /// <summary>
        /// The connection to the current data table is cached so we don't open unnecessary connections
        /// </summary>
        private async Task<CloudTable> GetCurrentDataTable(string hlaNomenclatureVersion)
        {
            if (cloudTable == null)
            {
                var dataTableReference = await tableReferenceRepository.GetCurrentTableReference(VersionedTableReferencePrefix(hlaNomenclatureVersion));
                cloudTable = await tableFactory.GetTable(dataTableReference);
            }
            return cloudTable;
        }

        private static TTableRow GetDataFromCache(
            string partition, string rowKey, IReadOnlyDictionary<string, TTableRow> metadataDictionary)
        {
            metadataDictionary.TryGetValue(partition + rowKey, out var row);
            return row;
        }

        private async Task<CloudTable> CreateNewDataTable(string tablePrefix)
        {
            var dataTableReference = tableReferenceRepository.GetNewTableReference(tablePrefix);
            return await tableFactory.GetTable(dataTableReference);
        }

        private static async Task InsertIntoDataTable(IEnumerable<TStorable> contents, CloudTable dataTable)
        {
            var partitionedEntities = contents
                .Select(data => new HlaMetadataTableRow(data))
                .GroupBy(ent => ent.PartitionKey)
                .ToList();

            foreach (var partition in partitionedEntities)
            {
                var entitiesInCurrentPartition = partition.ToList();
                
                await dataTable.BatchInsert(entitiesInCurrentPartition);
            }
        }
    }
}
