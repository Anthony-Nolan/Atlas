using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Models;

namespace Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models
{
    public class Mac
    {
        public string MacCode { get; set; }
        public string Hla { get; set; }
        public bool IsGeneric { get; set; }

        internal Mac(string macCode, string hla, bool isGeneric)
        {
            MacCode = macCode;
            Hla = hla;
            IsGeneric = isGeneric;
        }

        internal Mac(MacEntity mac)
        {
            MacCode = mac.Mac;
            Hla = mac.HLA;
            IsGeneric = mac.IsGeneric;
        }
    }
}