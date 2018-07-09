using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.Common.Exceptions;

namespace Nova.SearchAlgorithm.Common.Repositories
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
                catch (Exception ex)
                {
                    throw new CloudTableBatchInsertException(batchToInsert, ex);
                }
            }
        }
    }
}
