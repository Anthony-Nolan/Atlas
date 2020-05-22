using System;
using Microsoft.Azure.Cosmos.Table;

namespace Atlas.MultipleAlleleCodeDictionary.Models
{
    public class MacCode : TableEntity
    {
        public MacCode()
        {
            
        }
        public MacCode(string mac, string hla, bool isGeneric = false)
        {
            PartitionKey = "MAC";
            RowKey = mac;
            HLA = hla;
            IsGeneric = isGeneric; 
        }
        public string HLA { get; set; }
        public bool IsGeneric { get; set; }
    }
}