using Microsoft.WindowsAzure.Storage.Table;

namespace Atlas.HlaMetadataDictionary.Models
{
    internal class TableReferenceTableEntity : TableEntity
    {
        public string TableReference { get; set; }

        private const string PartitionValue = "TableNames";

        public TableReferenceTableEntity() { }

        public TableReferenceTableEntity(string tablePrefix, string tableReference): base(GetPartition(), tablePrefix)
        {
            TableReference = tableReference;
        }

        public static string GetPartition()
        {
            return PartitionValue;
        }
    }
}