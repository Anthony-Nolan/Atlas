using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    public static class CloudTableExtensions
    {
        private const int BatchSize = 100;

        public static async Task<TEntity> GetEntityByPartitionAndRowKey<TEntity>(this CloudTable table, string partition, string rowKey)
            where TEntity : TableEntity
        {
            var retrieveOperation = TableOperation.Retrieve<TEntity>(partition, rowKey);
            var tableResult = await table.ExecuteAsync(retrieveOperation);
            return (TEntity)tableResult.Result;
        }

        public static void BatchInsert<TEntity>(this CloudTable table, IEnumerable<TEntity> entities)
            where TEntity : TableEntity
        {
            var entitiesList = entities.ToList();
            for (var i = 0; i < entitiesList.Count; i = i + BatchSize)
            {
                var batchToInsert = entitiesList.Skip(i).Take(BatchSize).ToList();
                var batchOperation = new TableBatchOperation();
                batchToInsert.ForEach(entity => batchOperation.Insert(entity));

                try
                {
                    table.ExecuteBatch(batchOperation);
                }
                catch (StorageException ex)
                {
                    throw new CloudTableBatchInsertException(batchToInsert, ex);
                }
            }
        }
    }
}
