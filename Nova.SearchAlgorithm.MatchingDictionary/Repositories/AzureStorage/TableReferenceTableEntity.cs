using Microsoft.WindowsAzure.Storage.Table;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    public class TableReferenceTableEntity : TableEntity
    {
        public string MatchingDictionaryTableReference { get; set; }

        private const string PartitionValue = "Partition";
        private const string RowKeyValue = "RowKey";

        public TableReferenceTableEntity() { }

        public TableReferenceTableEntity(string tableReference): base(GetPartition(), GetRowKey())
        {
            MatchingDictionaryTableReference = tableReference;
        }

        public static string GetPartition()
        {
            return PartitionValue;
        }

        public static string GetRowKey()
        {
            return RowKeyValue;
        }
    }
}