using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;
using Microsoft.Azure.Cosmos.Table;

namespace Atlas.MultipleAlleleCodeDictionary.AzureStorage.Models
{
    internal class MacEntity : TableEntity
    {
        // ReSharper disable once UnusedMember.Global Needed for some Cosmos API methods
        public MacEntity() {}

        public MacEntity(Mac mac)
        {
            MacLength = mac.Code.Length;
            Mac = mac.Code;
            HLA = mac.Hla;
            IsGeneric = mac.IsGeneric;

            RowKey = Mac;
            PartitionKey = MacLength.ToString();
        }

        public string Mac { get; set; }
        public int MacLength { get; set; }
        public string HLA { get; set; }
        public bool IsGeneric { get; set; }
    }
}