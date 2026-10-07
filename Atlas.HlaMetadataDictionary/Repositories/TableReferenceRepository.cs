using System;
using System.Globalization;
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

        /// <param name="tablePrefix">The functional prefix and HLA nomenclature version of the table.</param>
        /// <param name="snapshotUtc">
        /// Ends the new table's name, so that tables written by the same recreation can be identified as one snapshot.
        /// </param>
        string GetNewTableReference(string tablePrefix, DateTime snapshotUtc);

        Task UpdateTableReference(string tablePrefix, string tableReference);
    }

    internal class TableReferenceRepository : ITableReferenceRepository
    {
        private readonly ITableClientFactory factory;
        private const string CloudTableReference = "TableReferences";

        /// <summary>
        /// 24-hour clock, so that names sort in the order the tables were created.
        /// </summary>
        private const string SnapshotFormat = "yyyyMMddHHmmssfff";

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

        public string GetNewTableReference(string tablePrefix, DateTime snapshotUtc)
        {
            return tablePrefix + snapshotUtc.ToString(SnapshotFormat, CultureInfo.InvariantCulture);
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
            var newReference = GetNewTableReference(tablePrefix, DateTime.UtcNow);
            await UpdateTableReference(tablePrefix, newReference);
            return newReference;
        }
    }
}
