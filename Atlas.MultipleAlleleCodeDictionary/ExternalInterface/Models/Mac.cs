using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Models;
using Atlas.MultipleAlleleCodeDictionary.Services;

namespace Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models
{
    public class Mac : IHasMacCode
    {
        public string Code { get; set; }
        public string Hla { get; set; }
        public bool IsGeneric { get; set; }

        public Mac()
        {
        }
        
        internal Mac(string code, string hla, bool isGeneric)
        {
            Code = code;
            Hla = hla;
            IsGeneric = isGeneric;
        }

        internal Mac(MacEntity mac)
        {
            Code = mac.Code;
            Hla = mac.HLA;
            IsGeneric = mac.IsGeneric;
        }
    }
}