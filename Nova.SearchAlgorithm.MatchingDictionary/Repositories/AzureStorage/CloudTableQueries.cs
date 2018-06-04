using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    internal static class CloudTableQueries
    {
        public static async Task<TEntity> RetrieveResultFromTableByPartitionAndRowKey<TEntity>(string partition, string rowKey, CloudTable table) where TEntity : TableEntity
        {
            var retrieveOperation = TableOperation.Retrieve<TEntity>(partition, rowKey);
            var tableResult = await table.ExecuteAsync(retrieveOperation);
            return (TEntity)tableResult.Result;
        }
    }
}
