using System;
using System.Threading.Tasks;
using Atlas.HlaMetadataDictionary.InternalModels;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;
using Azure.Data.Tables;

namespace Atlas.HlaMetadataDictionary.Repositories
{
    /// <summary>
    /// Holds the current table reference of various data tables.
    /// Some of our tables have their contents entirely regenerated from time to time.
    /// Since deleting a table in azure is not immediate or even synchronous, we instead
    /// hold a reference via this repository to the "current" version of the table,
    /// generate a new table from scratch, then update the reference when done.
    /// </summary>
    internal interface ITableReferenceRepository
    {
        Task<string> GetCurrentTableReference(string tablePrefix);
        string GetNewTableReference(string tablePrefix);
        Task UpdateTableReference(string tablePrefix, string tableReference);
    }

    internal class TableReferenceRepository : ITableReferenceRepository
    {
        private readonly ITableClientFactory factory;
        private const string CloudTableReference = "TableReferences";
        private TableClient tableClient;

        public TableReferenceRepository(ITableClientFactory factory)
        {
            this.factory = factory;
        }
        
        public async Task<string> GetCurrentTableReference(string tablePrefix)
        {
            var tableReferenceRow = await GetExistingTableReferenceRow(tablePrefix);

            return tableReferenceRow != null 
                ? tableReferenceRow.TableReference
                : await InsertAndReturnNewTableReference(tablePrefix);
        }

        public string GetNewTableReference(string tablePrefix)
        {
            var timeStamp = $"{DateTime.Now:yyyyMMddhhmmssfff}";
            return tablePrefix + timeStamp;
        }

        public async Task UpdateTableReference(string tablePrefix, string tableReference)
        {
            var tableClient = await GetTableClient();
            await tableClient.UpsertEntityAsync<TableReferenceRow>(new TableReferenceRow(tablePrefix, tableReference), TableUpdateMode.Replace);
        }

        private async Task<TableClient> GetTableClient()
        {
            return tableClient ??= await factory.GetTable(CloudTableReference);
        }

        private async Task<TableReferenceRow> GetExistingTableReferenceRow(string tablePrefix)
        {
            var client = await GetTableClient();
            var partition = TableReferenceRow.GetPartition();
            var rowKey = tablePrefix;

            var response = await client.GetEntityIfExistsAsync<TableReferenceRow>(partition, rowKey); 
            return response.HasValue ? response.Value : default;
        }

        private async Task<string> InsertAndReturnNewTableReference(string tablePrefix)
        {           
            var newReference = GetNewTableReference(tablePrefix);
            await UpdateTableReference(tablePrefix, newReference);
            return newReference;
        }
    }
}
