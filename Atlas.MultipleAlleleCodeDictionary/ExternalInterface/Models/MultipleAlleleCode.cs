using Atlas.MultipleAlleleCodeDictionary.Models;

namespace Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models
{
    public class MultipleAlleleCode
    {
        public string Mac { get; set; }
        public string Hla { get; set; }
        public bool IsGeneric { get; set; }

        internal MultipleAlleleCode(MultipleAlleleCodeEntity macEntity)
        {
            Mac = macEntity.Mac;
            Hla = macEntity.HLA;
            IsGeneric = macEntity.IsGeneric;
        }

        internal MultipleAlleleCode(string mac, string hla, bool isGeneric)
        {
            Mac = mac;
            Hla = hla;
            IsGeneric = isGeneric;
        }
    }
}