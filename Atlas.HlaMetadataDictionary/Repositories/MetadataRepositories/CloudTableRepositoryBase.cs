using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.AzureStorage.TableStorage;
using Atlas.Common.Caching;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.InternalExceptions;
using Atlas.HlaMetadataDictionary.InternalModels.MetadataTableRows;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;
using Azure.Data.Tables;
using LazyCache;

namespace Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories
{
    /// <summary>
    /// Generic repository that persists data to a TableClient
    /// & also caches it in memory for optimal read-access.
    /// </summary>
    internal interface IWarmableRepository
    {
        Task LoadDataIntoMemory(string hlaNomenclatureVersion);
    }

    internal abstract class TableClientRepositoryBase<TStorable, TTableRow> :
        IWarmableRepository
        where TTableRow : HlaMetadataTableRow, new()
        where TStorable : ISerialisableHlaMetadata
    {
        protected readonly IAppCache Cache;
        protected readonly ILogger Logger;

        private readonly ITableClientFactory tableFactory;
        private readonly ITableReferenceRepository tableReferenceRepository;
        private readonly string functionalTableReferencePrefix;
        private readonly string cacheKey;
        private readonly IDictionary<string, TableClient> tableClients = new Dictionary<string, TableClient>();

        protected TableClientRepositoryBase(
            ITableClientFactory factory,
            ITableReferenceRepository tableReferenceRepository,
            string functionalTableReferencePrefix,
            // ReSharper disable once SuggestBaseTypeForParameter
            IPersistentCacheProvider cacheProvider,
            string cacheKey,
            ILogger logger)
        {
            tableFactory = factory;
            this.tableReferenceRepository = tableReferenceRepository;
            this.functionalTableReferencePrefix = functionalTableReferencePrefix;
            Cache = cacheProvider.Cache;
            this.cacheKey = cacheKey;
            Logger = logger;
        }

        /// <summary>
        /// Pre-warms the in-memory cache of all metadata for the specified nomenclature version.
        /// While the cache will be lazily warmed on first request, this can be called up-front
        /// to e.g. ensure that the first real request of the day is not unnecessarily slow.   
        /// </summary>
        public async Task LoadDataIntoMemory(string hlaNomenclatureVersion)
        {
            await TableData(hlaNomenclatureVersion);
        }

        protected async Task RecreateDataTable(IEnumerable<TStorable> tableContents, string hlaNomenclatureVersion)
        {
            var tablePrefix = VersionedTableReferencePrefix(hlaNomenclatureVersion);
            var newDataTable = await CreateNewDataTable(tablePrefix);


            await newDataTable.BatchInsert(tableContents.Select(rowData => new HlaMetadataTableRow(rowData).ToTableEntity()));
            await tableReferenceRepository.UpdateTableReference(tablePrefix, newDataTable.Name);
            tableClients.Remove(tablePrefix);
        }

        protected async Task<TTableRow> GetDataRowIfExists(string partition, string rowKey, string hlaNomenclatureVersion)
        {
            return await Cache.GetSingleItemAndScheduleWholeCollectionCacheWarm(
                VersionedCacheKey(hlaNomenclatureVersion),
                () => FetchAllRowsInTable(hlaNomenclatureVersion),
                tableDictionary => GetRowFromCachedTable(partition, rowKey, tableDictionary),
                () => FetchRowFromSourceTable(partition, rowKey, hlaNomenclatureVersion)
            );
        }

        protected async Task<Dictionary<string, TTableRow>> TableData(string hlaNomenclatureVersion)
        {
            return await Cache.GetOrAddWholeCollectionAsync_Tracked(VersionedCacheKey(hlaNomenclatureVersion), () => FetchAllRowsInTable(hlaNomenclatureVersion))
                   ?? throw new MemoryCacheException($"HLA metadata could not be loaded for nomenclature version: {hlaNomenclatureVersion}");
        }

        private string VersionedCacheKey(string hlaNomenclatureVersion) => $"{cacheKey}:{hlaNomenclatureVersion}";

        private async Task<Dictionary<string, TTableRow>> FetchAllRowsInTable(string hlaNomenclatureVersion)
        {
            var operationDescription = $"Fetch and cache Hla Metadata Dictionary data: {cacheKey} at version: '{hlaNomenclatureVersion}'.";
            using (Logger.RunTimed(operationDescription))
            {
                var currentDataTable = await GetVersionedDataTable(hlaNomenclatureVersion);
                //var tableResults = new CloudTableBatchQueryAsync<TTableRow>(currentDataTable);
                var dataToLoad = new Dictionary<string, TTableRow>();
                var pages = currentDataTable.QueryAsync<TableEntity>(x => true).AsPages();

                await foreach (var page in pages)
                {
                    foreach (var result in page.Values)
                    {
                        var row = new TTableRow();
                        row.ReadEntity(result);

                        dataToLoad.Add(RowPrimaryKey(result.PartitionKey, result.RowKey), row);
                    }
                }

                return dataToLoad;
            }
        }

        private static string RowPrimaryKey(string partitionKey, string rowKey) => partitionKey + rowKey;

        private string VersionedTableReferencePrefix(string hlaNomenclatureVersion)
        {
            return $"{functionalTableReferencePrefix}{hlaNomenclatureVersion}";
        }

        private readonly SemaphoreSlim tableConnectionCreationLock = new(1, 1);

        /// <summary>
        /// The connection to the current data table is cached so we don't open unnecessary connections
        /// </summary>
        private async Task<TableClient> GetVersionedDataTable(string hlaNomenclatureVersion)
        {
            await tableConnectionCreationLock.WaitAsync();

            var tablePrefix = VersionedTableReferencePrefix(hlaNomenclatureVersion);

            try
            {
                if (tableClients.TryGetValue(tablePrefix, out var cachedTableClient))
                {
                    return cachedTableClient;
                }

                var dataTableReference = await tableReferenceRepository.GetCurrentTableReference(tablePrefix);
                var TableClient = await tableFactory.GetTable(dataTableReference);
                tableClients.Add(tablePrefix, TableClient);
                return TableClient;
            }
            finally
            {
                tableConnectionCreationLock.Release();
            }
        }

        private static TTableRow GetRowFromCachedTable(string partition, string rowKey, IReadOnlyDictionary<string, TTableRow> metadataDictionary)
        {
            metadataDictionary.TryGetValue(RowPrimaryKey(partition, rowKey), out var row);
            return row;
        }

        private async Task<TTableRow> FetchRowFromSourceTable(string partition, string rowKey, string hlaNomenclatureVersion)
        {
            var table = await GetVersionedDataTable(hlaNomenclatureVersion);
            var tableEntity = await table.GetByPartitionAndRowKey<TableEntity>(partition, rowKey);

            if (tableEntity == null)
                return null;

            var item = new TTableRow();
            item.ReadEntity(tableEntity);
            return item;
        }

        private async Task<TableClient> CreateNewDataTable(string tablePrefix)
        {
            var dataTableReference = tableReferenceRepository.GetNewTableReference(tablePrefix);
            return await tableFactory.GetTable(dataTableReference);
        }
    }
}