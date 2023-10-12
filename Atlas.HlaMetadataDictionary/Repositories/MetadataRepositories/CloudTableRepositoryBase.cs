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
using LazyCache;
using Microsoft.Azure.Cosmos.Table;

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
        protected readonly IAppCache Cache;
        protected readonly ILogger Logger;

        private readonly ICloudTableFactory tableFactory;
        private readonly ITableReferenceRepository tableReferenceRepository;
        private readonly string functionalTableReferencePrefix;
        private readonly string cacheKey;
        private readonly IDictionary<string, CloudTable> cloudTables = new Dictionary<string, CloudTable>();

        protected CloudTableRepositoryBase(
            ICloudTableFactory factory,
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
            await newDataTable.BatchInsert(tableContents.Select(rowData => new HlaMetadataTableRow(rowData)));
            await tableReferenceRepository.UpdateTableReference(tablePrefix, newDataTable.Name);
            cloudTables.Remove(tablePrefix);
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
                var tableResults = new CloudTableBatchQueryAsync<TTableRow>(currentDataTable);
                var dataToLoad = new Dictionary<string, TTableRow>();

                while (tableResults.HasMoreResults)
                {
                    var results = await tableResults.RequestNextAsync();
                    foreach (var result in results)
                    {
                        dataToLoad.Add(RowPrimaryKey(result.PartitionKey, result.RowKey), result);
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
        private async Task<CloudTable> GetVersionedDataTable(string hlaNomenclatureVersion)
        {
            await tableConnectionCreationLock.WaitAsync();

            var tablePrefix = VersionedTableReferencePrefix(hlaNomenclatureVersion);

            try
            {
                if (cloudTables.TryGetValue(tablePrefix, out var cachedCloudTable))
                {
                    return cachedCloudTable;
                }

                var dataTableReference = await tableReferenceRepository.GetCurrentTableReference(tablePrefix);
                var cloudTable = await tableFactory.GetTable(dataTableReference);
                cloudTables.Add(tablePrefix, cloudTable);
                return cloudTable;
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
            return await table.GetByPartitionAndRowKey<TTableRow>(partition, rowKey);
        }

        private async Task<CloudTable> CreateNewDataTable(string tablePrefix)
        {
            var dataTableReference = tableReferenceRepository.GetNewTableReference(tablePrefix);
            return await tableFactory.GetTable(dataTableReference);
        }
    }
}