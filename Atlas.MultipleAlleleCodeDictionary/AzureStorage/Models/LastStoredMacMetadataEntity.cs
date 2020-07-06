using Atlas.MultipleAlleleCodeDictionary.Services;
using Microsoft.Azure.Cosmos.Table;

namespace Atlas.MultipleAlleleCodeDictionary.AzureStorage.Models
{
    internal class LastStoredMacMetadataEntity : TableEntity, IHasMacCode
    {
        public const string MetadataPartitionKey = "Metadata"; 
        public const string LatestMacRowKey = "LastImported"; 
        
        public LastStoredMacMetadataEntity()
        {
            RowKey = LatestMacRowKey;
            PartitionKey = MetadataPartitionKey;
        }

        /// <inheritdoc />
        public string Code { get; set; }
    }
}