using Atlas.Common.AzureStorage;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;
using Atlas.MultipleAlleleCodeDictionary.Services;
using Azure;
using Azure.Data.Tables;
using System;

namespace Atlas.MultipleAlleleCodeDictionary.AzureStorage.Models
{
    /// <remarks>
    /// Note that this class is used to deserialise the FileBackedMacDictionary, as well as the Azure Storage
    /// If you change this, you'll need to work out how to regenerate the file, so that it can be interpretted.
    /// See notes in <see cref="FileBackedMacDictionaryRepository"/>
    /// </remarks>
    internal class MacEntity : AtlasTableEntityBase, IHasMacCode
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