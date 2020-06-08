using Microsoft.Azure.Cosmos.Table;

namespace Atlas.MultipleAlleleCodeDictionary.AzureStorage.Models
{
    internal class MultipleAlleleCodeEntity : TableEntity
    {
        public MultipleAlleleCodeEntity()
        {
        }

        public MultipleAlleleCodeEntity(string mac, string hla, bool isGeneric = false)
        {
            PartitionKey = mac.Length.ToString();
            RowKey = mac;
            HLA = hla;
            IsGeneric = isGeneric;
        }

        public string HLA { get; set; }
        public bool IsGeneric { get; set; }
        
        public string Mac => RowKey;
    }
}