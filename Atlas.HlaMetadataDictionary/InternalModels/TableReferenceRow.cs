using Atlas.Common.AzureStorage;
using System;

namespace Atlas.HlaMetadataDictionary.InternalModels
{
    internal class TableReferenceRow : AtlasTableEntityBase
    {
        public string TableReference { get; set; }

        private const string PartitionValue = "TableNames";

        public TableReferenceRow() { }

        public TableReferenceRow(string tablePrefix, string tableReference)
        {
            TableReference = tableReference;
            RowKey = tablePrefix;
            PartitionKey = GetPartition();
        }

        public static string GetPartition()
        {
            return PartitionValue;
        }
    }
}