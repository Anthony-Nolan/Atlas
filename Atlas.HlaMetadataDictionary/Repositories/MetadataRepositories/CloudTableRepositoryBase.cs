using System;
using System.Collections.Generic;
using System.Linq;
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
        protected readonly IAtlasLogger AtlasLogger;

        private readonly ITableClientFactory tableFactory;
        private readonly ITableReferenceRepository tableReferenceRepository;
        private readonly string functionalTableReferencePrefix;
        private readonly string cacheKey;

        protected TableClientRepositoryBase(
            ITableClientFactory factory,
            ITableReferenceRepository tableReferenceRepository,
            string functionalTableReferencePrefix,
            // ReSharper disable once SuggestBaseTypeForParameter
            IPersistentCacheProvider cacheProvider,
            string cacheKey,
            IAtlasLogger logger)
        {
            tableFactory = factory;
            this.tableReferenceRepository = tableReferenceRepository;
            this.functionalTableReferencePrefix = functionalTableReferencePrefix;
            Cache = cacheProvider.Cache;
            this.cacheKey = cacheKey;
            AtlasLogger = logger;
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

        protected async Task RecreateDataTable(IEnumerable<TStorable> tableContents, string hlaNomenclatureVersion, DateTime snapshotUtc)
        {
            var tablePrefix = VersionedTableReferencePrefix(hlaNomenclatureVersion);
            var newDataTable = await CreateNewDataTable(tablePrefix, snapshotUtc);


            await newDataTable.BatchInsert(tableContents.Select(rowData => new HlaMetadataTableRow(rowData).ToTableEntity()));
            await tableReferenceRepository.UpdateTableReference(tablePrefix, newDataTable.Name);
            Cache.Remove(TableClientCacheKey(hlaNomenclatureVersion));
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
            using (AtlasLogger.RunTimed(operationDescription))
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

        /// <summary>
        /// The connection to the current data table, cached so we don't open unnecessary connections - creating one
        /// costs a round trip, and this is on the path of every single-item lookup made while a collection warms.
        /// </summary>
        /// <remarks>
        /// Held under a version-keyed cache entry rather than in a field, so that invalidating a version drops its
        /// table client along with its data. Recreation writes a NEW physical table and leaves the old one in place,
        /// so a client cached per repository instance would outlive the eviction: an object graph obtained before the
        /// recreation would miss the cache, re-read the OLD table it still points at, and store those rows again
        /// under the same versioned key for a further cache lifetime.
        /// </remarks>
        private async Task<TableClient> GetVersionedDataTable(string hlaNomenclatureVersion)
        {
            return await Cache.GetOrAddAsync(TableClientCacheKey(hlaNomenclatureVersion), async () =>
            {
                var tablePrefix = VersionedTableReferencePrefix(hlaNomenclatureVersion);
                var dataTableReference = await tableReferenceRepository.GetCurrentTableReference(tablePrefix);

                return await tableFactory.GetTable(dataTableReference);
            });
        }

        /// <remarks>
        /// Ends with ":{version}" so that <c>HlaVersionedCacheKey</c> recognises it and the existing invalidation
        /// evicts it; nothing separate has to know about table clients.
        /// </remarks>
        private string TableClientCacheKey(string hlaNomenclatureVersion) => $"{cacheKey}-tableClient:{hlaNomenclatureVersion}";

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

        private async Task<TableClient> CreateNewDataTable(string tablePrefix, DateTime snapshotUtc)
        {
            var dataTableReference = tableReferenceRepository.GetNewTableReference(tablePrefix, snapshotUtc);
            return await tableFactory.GetTable(dataTableReference);
        }
    }
}