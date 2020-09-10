using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Cosmos.Table.Protocol;
using Microsoft.Azure.Cosmos.Table.Queryable;
using MoreLinq;

namespace Atlas.Common.AzureStorage.TableStorage
{
    public static class AzureCosmosLibraryTableExtensions
    {
        public static async Task<IList<T>> ExecuteQueryAsync<T>(
            this CloudTable table,
            TableQuery<T> query,
            CancellationToken ct = default,
            Action<IList<T>> onProgress = null)
            where T : ITableEntity, new()
        {
            var items = new List<T>();
            TableContinuationToken token = null;

            do
            {
                var seg = await table.ExecuteQuerySegmentedAsync<T>(query, token, ct);
                token = seg.ContinuationToken;
                items.AddRange(seg);
                onProgress?.Invoke(items);
            } while (token != null && !ct.IsCancellationRequested);

            return items;
        }

        //ExecuteBatchAsync has a limit on how many operations can be put in a single batch. :(
        private const int BatchSize = TableConstants.TableServiceBatchMaximumOperations;

        /*
         * Note that the internet recommends the following settings for optimal AzureTableStorage Insert performance.
         * However, applying them didn't appear to have any impact. Possibly because we're batching all our inserts, which negates the issues these address?
         *
         * ServicePointManager.Expect100Continue = false;
         * ServicePointManager.UseNagleAlgorithm = false;
         * ServicePointManager.DefaultConnectionLimit = 100;
         */
        /// <summary>
        /// Splits entities by partition before applying batch inserts - so can be called without first splitting by partition.
        /// </summary>
        public static async Task BatchInsert<TEntity>(this CloudTable table, IEnumerable<TEntity> entities)
            where TEntity : TableEntity
        {
            // ReSharper disable once SuggestVarOrType_Elsewhere
            // List<List<List<TEntities>>> 
            // This construct is:
            // * A List of Partitions, each containing
            //   * List of 100-entity-batches within that partition, each containing
            //     * List of (100) Entities within that batch (all of which have a single PartitionKey)
            //
            // We start from this construct as it allows easy refactors of how we divvy these inserts up, in the future.
            List<List<List<TEntity>>> entitiesPartitionedWithSubBatches = entities
                .GroupBy(e => e.PartitionKey) //Note that batch inserts MUST have a common Partition Key.
                .Select(partitionGroup => partitionGroup.Batch(BatchSize).Select(batch => batch.ToList()).ToList())
                .ToList();

            foreach (var subBatchesWithinASinglePartition in entitiesPartitionedWithSubBatches)
            {
                foreach (var batchToInsert in subBatchesWithinASinglePartition)
                {
                    var batchOperation = new TableBatchOperation();
                    foreach (var tableEntity in batchToInsert)
                    {
                        batchOperation.Insert(tableEntity);
                    }

                    try
                    {
                        await table.ExecuteBatchAsync(batchOperation);
                    }
                    catch (StorageException ex)
                    {
                        throw new AzureTableBatchInsertException(batchToInsert, ex);
                    }
                }
            }
        }

        /// <returns>
        /// The entity represented by the given composite primary key (comprised of partitionKey and rowKey).
        /// If no entity with matching keys is found, returns null. 
        /// </returns>
        public static async Task<TEntity> GetByPartitionAndRowKey<TEntity>(this CloudTable table, string partitionKey, string rowKey)
            where TEntity : TableEntity, new()
        {
            var query = table.CreateQuery<TEntity>()
                .Where(m => m.PartitionKey == partitionKey)
                .Where(m => m.RowKey == rowKey);

            return (await table.ExecuteQueryAsync(query.AsTableQuery())).SingleOrDefault();
        }
    }
}