using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Models;

namespace Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models
{
    public class MultipleAlleleCode
    {
        public string Mac { get; set; }
        public string Hla { get; set; }
        public bool IsGeneric { get; set; }

        internal MultipleAlleleCode(string mac, string hla, bool isGeneric)
        {
            Mac = mac;
            Hla = hla;
            IsGeneric = isGeneric;
        }

        internal MultipleAlleleCode(MultipleAlleleCodeEntity mac)
        {
            Mac = mac.Mac;
            Hla = mac.HLA;
            IsGeneric = mac.IsGeneric;
        }
    }
}