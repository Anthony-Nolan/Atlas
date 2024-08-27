using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Data.Tables;
using Dasync.Collections;
using MoreLinq;

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


        //ExecuteBatchAsync has a limit on how many operations can be put in a single batch. :(
        private const int BatchSize = 100;

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