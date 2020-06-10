using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;
using Microsoft.Azure.Cosmos.Table;

namespace Atlas.MultipleAlleleCodeDictionary.AzureStorage.Models
{
    internal class MacEntity : TableEntity
    {
        public MacEntity()
        {
        }

        public MacEntity(Mac mac)
        {
            PartitionKey = mac.MacCode.Length.ToString();
            RowKey = mac.MacCode;
            HLA = mac.Hla;
            IsGeneric = mac.IsGeneric;
        }

        public string HLA { get; set; }
        public bool IsGeneric { get; set; }
        
        public string Mac => RowKey;
        public string MacLength => PartitionKey;
    }
}