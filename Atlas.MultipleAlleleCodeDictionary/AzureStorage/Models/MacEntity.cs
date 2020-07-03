using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;
using Atlas.MultipleAlleleCodeDictionary.Services;
using Microsoft.Azure.Cosmos.Table;

namespace Atlas.MultipleAlleleCodeDictionary.AzureStorage.Models
{
    internal class MacEntity : TableEntity, IHasMacCode
    {
        // ReSharper disable once UnusedMember.Global Needed for some Cosmos API methods
        public MacEntity() {}

        public MacEntity(Mac mac)
        {
            Code = mac.Code;
            HLA = mac.Hla;
            IsGeneric = mac.IsGeneric;

            //TODO: ATLAS-488. Rationalise these.
            RowKey = Code;
            PartitionKey = Code.Length.ToString();
        }

        public string Code { get; set; }
        public string HLA { get; set; }
        public bool IsGeneric { get; set; }
    }
}