using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.HlaMetadataDictionary.InternalExceptions;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using MoreLinq;

namespace Atlas.HlaMetadataDictionary.Repositories.AzureStorage
{
    internal static class CloudTableExtensions
    {
        private const int BatchSize = 100; //ExecuteBatchAsync is limited to 100 operations per batch. :(

        public static async Task<TRow> GetRowByPartitionAndRowKey<TRow>(this CloudTable table, string partition, string rowKey)
            where TRow : TableEntity
        {
            var retrieveOperation = TableOperation.Retrieve<TRow>(partition, rowKey);
            var tableResult = await table.ExecuteAsync(retrieveOperation);
            return (TRow)tableResult.Result;
        }

        public static async Task BatchInsert<TEntity>(this CloudTable table, IEnumerable<TEntity> entities)
            where TEntity : TableEntity
        {
            var entityBatches = entities.Batch(BatchSize).ToList();
            foreach (var batchToInsert in entityBatches)
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
                    throw new CloudTableBatchInsertException(batchToInsert, ex);
                }
            }
        }
    }
}
