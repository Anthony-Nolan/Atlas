using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;

namespace Atlas.Common.AzureStorage.TableStorage
{
    public static class AzureCosmosLibraryTableExtensions
    {
        public static async Task<IList<T>> ExecuteQueryAsync<T>(
            this TableClient tableClient,
            string filter,
            CancellationToken ct = default,
            Action<IList<T>> onProgress = null)
            where T : class, ITableEntity, new()
        {
            return await tableClient.QueryAsync<T>(filter, cancellationToken: ct).ToListAsync();
        }


        // A table transaction may hold at most 100 operations, and at most 4 MiB of request body.
        private const int BatchSize = 50;

        /// <summary>
        /// The most a batch may hold, by the estimate in <see cref="EstimatedRequestBytes"/>. Kept well under the 4 MiB limit, because
        /// the estimate does not reproduce the exact request body that the SDK writes.
        /// </summary>
        internal const int MaxBatchRequestBytes = 3 * 1024 * 1024;

        /// <summary>
        /// Allows for the multipart headers and the request line (which holds the keys) of each operation in the batch.
        /// </summary>
        private const int RequestOverheadBytesPerEntity = 1024;

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
        public static async Task BatchInsert<TEntity>(this TableClient table, IEnumerable<TEntity> entities)
            where TEntity : class, ITableEntity
        {
            // ReSharper disable once SuggestVarOrType_Elsewhere
            // List<List<List<TEntities>>> 
            // This construct is:
            // * A List of Partitions, each containing
            //   * List of batches within that partition, each containing
            //     * List of (up to 50) Entities within that batch (all of which have a single PartitionKey)
            //
            // We start from this construct as it allows easy refactors of how we divvy these inserts up, in the future.
            List<List<List<TEntity>>> entitiesPartitionedWithSubBatches = entities
                .GroupBy(e => e.PartitionKey) //Note that batch inserts MUST have a common Partition Key.
                .Select(partitionGroup => SplitIntoBatches(partitionGroup))
                .ToList();

            foreach (var subBatchesWithinASinglePartition in entitiesPartitionedWithSubBatches)
            {
                foreach (var batchToInsert in subBatchesWithinASinglePartition)
                {
                    var transaction = batchToInsert
                        .Select(x => new TableTransactionAction(TableTransactionActionType.Add, x))
                        .ToList();
                    try
                    {
                        await table.SubmitTransactionAsync(transaction);
                    }
                    catch (RequestFailedException ex)
                    {
                        throw new AzureTableBatchInsertException(batchToInsert, ex);
                    }
                }
            }
        }

        /// <summary>
        /// Starts a new batch when the current one is full, either by count or by size. Some entities are large (e.g. the HLA Metadata
        /// Dictionary's serology to alleles rows hold up to ~100 KB of serialised alleles each), so a count limit alone is not enough.
        /// An entity that is too large on its own still gets a batch of its own, and the storage service decides whether to accept it.
        /// </summary>
        private static List<List<TEntity>> SplitIntoBatches<TEntity>(IEnumerable<TEntity> entitiesInOnePartition)
            where TEntity : class, ITableEntity
        {
            var batches = new List<List<TEntity>>();
            var currentBatch = new List<TEntity>();
            var currentBatchBytes = 0L;

            foreach (var entity in entitiesInOnePartition)
            {
                var entityBytes = EstimatedRequestBytes(entity);
                if (currentBatch.Count > 0 && (currentBatch.Count == BatchSize || currentBatchBytes + entityBytes > MaxBatchRequestBytes))
                {
                    batches.Add(currentBatch);
                    currentBatch = new List<TEntity>();
                    currentBatchBytes = 0;
                }

                currentBatch.Add(entity);
                currentBatchBytes += entityBytes;
            }

            if (currentBatch.Count > 0)
            {
                batches.Add(currentBatch);
            }

            return batches;
        }

        /// <remarks>
        /// Serialises with the same default encoder as the SDK, which escapes characters such as '"' to six bytes each. This matters,
        /// because entities often hold serialised JSON as a string property.
        /// </remarks>
        internal static long EstimatedRequestBytes<TEntity>(TEntity entity) where TEntity : class, ITableEntity
        {
            return JsonSerializer.SerializeToUtf8Bytes(entity, entity.GetType()).LongLength + RequestOverheadBytesPerEntity;
        }

        /// <returns>
        /// The entity represented by the given composite primary key (comprised of partitionKey and rowKey).
        /// If no entity with matching keys is found, returns null. 
        /// </returns>
        public static async Task<TEntity> GetByPartitionAndRowKey<TEntity>(this TableClient table, string partitionKey, string rowKey)
            where TEntity : class, ITableEntity, new()
        {
            var response = await table.GetEntityIfExistsAsync<TEntity>(partitionKey, rowKey);
            return response.HasValue ? response.Value : default(TEntity);
        }   
    }
}