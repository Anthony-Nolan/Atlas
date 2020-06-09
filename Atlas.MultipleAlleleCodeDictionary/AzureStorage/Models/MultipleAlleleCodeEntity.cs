using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;
using Microsoft.Azure.Cosmos.Table;

namespace Atlas.MultipleAlleleCodeDictionary.AzureStorage.Models
{
    internal class MultipleAlleleCodeEntity : TableEntity
    {
        public MultipleAlleleCodeEntity()
        {
        }

        public MultipleAlleleCodeEntity(MultipleAlleleCode mac)
        {
            PartitionKey = mac.Mac.Length.ToString();
            RowKey = mac.Mac;
            HLA = mac.Hla;
            IsGeneric = mac.IsGeneric;
        }

        public string HLA { get; set; }
        public bool IsGeneric { get; set; }
        
        public string Mac => RowKey;
        public string MacLength => PartitionKey;
    }
}