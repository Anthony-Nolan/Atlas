using System.Collections.Generic;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Models;
using Atlas.MultipleAlleleCodeDictionary.Services;

namespace Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models
{
    public class Mac : IHasMacCode
    {
        private const char AlleleDelimiter = '/';

        public string Code { get; set; }
        public string Hla { get; set; }
        public bool IsGeneric { get; set; }

        public IEnumerable<string> SplitHla => Hla.Split(AlleleDelimiter);

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