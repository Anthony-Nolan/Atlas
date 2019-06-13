using System;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.MatchingDictionary.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories
{
    /// <summary>
    /// Holds the current table reference of various data tables.
    /// Some of our tables have their contents entirely regenerated from time to time.
    /// Since deleting a table in azure is not immediate or even synchronous, we instead
    /// hold a reference via this repository to the "current" version of the table,
    /// generate a new table from scratch, then update the reference when done.
    /// </summary>
    public interface ITableReferenceRepository
    {
        Task<string> GetCurrentTableReference(string tablePrefix);
        string GetNewTableReference(string tablePrefix);
        Task UpdateTableReference(string tablePrefix, string tableReference);
    }

    public class TableReferenceRepository : ITableReferenceRepository
    {
        private readonly ICloudTableFactory factory;
        private const string CloudTableReference = "TableReferences";
        private CloudTable table;

        public TableReferenceRepository(ICloudTableFactory factory)
        {
            this.factory = factory;
        }
        
        public async Task<string> GetCurrentTableReference(string tablePrefix)
        {
            var entity = await GetExistingTableEntity(tablePrefix);

            return entity != null 
                ? entity.TableReference
                : await InsertEntityAndReturnNewTableReference(tablePrefix);
        }

        public string GetNewTableReference(string tablePrefix)
        {
            var timeStamp = $"{DateTime.Now:yyyyMMddhhmmssfff}";
            return tablePrefix + timeStamp;
        }

        public async Task UpdateTableReference(string tablePrefix, string tableReference)
        {
            var insertOrReplaceOperation = TableOperation.InsertOrReplace(new TableReferenceTableEntity(tablePrefix, tableReference));
            await (await GetTable()).ExecuteAsync(insertOrReplaceOperation);
        }

        private async Task<CloudTable> GetTable()
        {
            return table ?? (table = await factory.GetTable(CloudTableReference));
        }

        private async Task<TableReferenceTableEntity> GetExistingTableEntity(string tablePrefix)
        {
            var partition = TableReferenceTableEntity.GetPartition();
            var rowKey = tablePrefix;
            var entity = await (await GetTable()).GetEntityByPartitionAndRowKey<TableReferenceTableEntity>(partition, rowKey);
            return entity;
        }

        private async Task<string> InsertEntityAndReturnNewTableReference(string tablePrefix)
        {           
            var newReference = GetNewTableReference(tablePrefix);
            await UpdateTableReference(tablePrefix, newReference);
            return newReference;
        }
    }
}
