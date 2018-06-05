using System;
using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories
{
    /// <summary>
    /// Holds the current table reference of the MatchingDictionary data table.
    /// </summary>
    public interface ITableReferenceRepository
    {
        Task<string> GetMatchingDictionaryTableReferenceIfExistsElseEmptyString();
        string CreateNewMatchingDictionaryTableReference();
        Task InsertOrUpdateMatchingDictionaryTableReference(string dataTableReference);
    }

    public class TableReferenceRepository : ITableReferenceRepository
    {
        private const string CloudTableReference = "MatchingDictionaryTableReference";
        private const string DataTableReferencePrefix = "MatchingDictionaryData";
        private readonly CloudTable table;

        public TableReferenceRepository(ICloudTableFactory factory)
        {
            table = factory.GetOrCreateTable(CloudTableReference);
        }

        public async Task<string> GetMatchingDictionaryTableReferenceIfExistsElseEmptyString()
        {
            var partition = TableReferenceTableEntity.GetPartition();
            var rowKey = TableReferenceTableEntity.GetRowKey();
            var entity = await table.GetEntityByPartitionAndRowKey<TableReferenceTableEntity>(partition, rowKey);

            if (entity != null)
            {
                return entity.MatchingDictionaryTableReference;
            }

            var emptyString = string.Empty;
            await InsertOrUpdateMatchingDictionaryTableReference(emptyString);
            return emptyString;
        }

        public string CreateNewMatchingDictionaryTableReference()
        {
            var timeStamp = $"{DateTime.Now:yyyyMMddhhmmssfff}";
            return DataTableReferencePrefix + timeStamp;
        }

        public async Task InsertOrUpdateMatchingDictionaryTableReference(string dataTableReference)
        {
            var insertOrReplaceOperation = TableOperation.InsertOrReplace(new TableReferenceTableEntity(dataTableReference));
            await table.ExecuteAsync(insertOrReplaceOperation);
        }
    }
}
