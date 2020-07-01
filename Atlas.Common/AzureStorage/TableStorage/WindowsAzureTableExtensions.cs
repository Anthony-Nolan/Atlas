using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using MoreLinq;

namespace Atlas.Common.AzureStorage.TableStorage
{
    public static class WindowsAzureTableExtensions
    {
        public static async Task<TRow> GetRowByPartitionAndRowKey<TRow>(this CloudTable table, string partition, string rowKey)
            where TRow : TableEntity
        {
            var retrieveOperation = TableOperation.Retrieve<TRow>(partition, rowKey);
            var tableResult = await table.ExecuteAsync(retrieveOperation);
            return (TRow)tableResult.Result;
        }

        private const int BatchSize = 100; //ExecuteBatchAsync has a limit on how many operations can be put in a single batch. :( Note that the constant TableConstants.TableServiceBatchMaximumOperations exists in the Cosmos Library.

        /*
         * Note that the internet recommends the following settings for optimal AzureTableStorage Insert performance.
         * However, applying them didn't appear to have any impact. Possibly because we're batching all our inserts, which negates the issues these address?
         *
         * ServicePointManager.Expect100Continue = false;
         * ServicePointManager.UseNagleAlgorithm = false;
         * ServicePointManager.DefaultConnectionLimit = 100;
         */
        public static async Task BatchInsert<TEntity>(this CloudTable table, IEnumerable<TEntity> entities)
            where TEntity : TableEntity
        {
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
    }
}
