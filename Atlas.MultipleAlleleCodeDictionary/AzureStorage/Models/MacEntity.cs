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

            RowKey = Code.AsRowKey();
            PartitionKey = Code.AsPartitionKey();
        }

        public string Code { get; set; }
        public string HLA { get; set; }
        public bool IsGeneric { get; set; }
    }

    internal static class MacEntityTableIdentifierExtensions
    {
        public static string AsRowKey(this string mac)
        {
            return mac;
        }

        public static string AsPartitionKey(this string mac)
        {
            return mac.Length.ToString();
        }
    }
}