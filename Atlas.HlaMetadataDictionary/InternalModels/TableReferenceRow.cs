using Microsoft.Azure.Cosmos.Table;

namespace Atlas.HlaMetadataDictionary.InternalModels
{
    internal class TableReferenceRow : TableEntity
    {
        public string TableReference { get; set; }

        private const string PartitionValue = "TableNames";

        public TableReferenceRow() { }

        public TableReferenceRow(string tablePrefix, string tableReference): base(GetPartition(), tablePrefix)
        {
            TableReference = tableReference;
        }

        public static string GetPartition()
        {
            return PartitionValue;
        }
    }
}