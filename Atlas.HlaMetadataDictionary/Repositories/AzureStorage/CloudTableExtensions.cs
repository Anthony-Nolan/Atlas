using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Atlas.HlaMetadataDictionary.Exceptions;
using MoreLinq;

namespace Atlas.HlaMetadataDictionary.Repositories.AzureStorage
{
    internal static class CloudTableExtensions
    {
        private const int BatchSize = 100; //ExecuteBatchAsync is limited to 100 operations per batch. :(

        public static async Task<TEntity> GetEntityByPartitionAndRowKey<TEntity>(this CloudTable table, string partition, string rowKey)
            where TEntity : TableEntity
        {
            var retrieveOperation = TableOperation.Retrieve<TEntity>(partition, rowKey);
            var tableResult = await table.ExecuteAsync(retrieveOperation);
            return (TEntity)tableResult.Result;
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
