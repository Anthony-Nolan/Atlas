using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;
using Atlas.Common.GeneticData.Hla.Models.MolecularHlaTyping;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.Helpers;
using Atlas.Common.GeneticData.Hla.Services.AlleleStringSplitters;

namespace Atlas.MultipleAlleleCodeDictionary.utils
{
    internal interface IMacExpander
    {
        MolecularAlleleDetails ExpandMac(MultipleAlleleCode mac, string firstField);
    }
    
    internal class MacExpander : IMacExpander
    {
        private const char AlleleDelimiter = '/';
        private const char FieldDelimiter = ':';
        private AlleleStringSplitterBase stringSplitter;

        public MacExpander(AlleleStringSplitterBase stringSplitter)
        {
            this.stringSplitter = stringSplitter;
        }
        
        public MolecularAlleleDetails ExpandMac(MultipleAlleleCode mac, string firstField)
        {
            return mac.IsGeneric ? ExpandMac(mac, firstField) : GetSpecificMac(mac);
        }
        private MolecularAlleleDetails GetGenericMac(MultipleAlleleCode mac, string firstField)
        {
            var secondFields = mac.Hla.Split(AlleleDelimiter);
            var combinedFields = new string[secondFields.Length];
            for (var i = 0; i < secondFields.Length; i++)
            {
                combinedFields[i] = $"{firstField}:{secondFields[i]}";
            }

            var alleles = string.Join(FieldDelimiter, combinedFields);
            return new MolecularAlleleDetails(alleles);
        }

        private MolecularAlleleDetails GetSpecificMac(MultipleAlleleCode mac)
        {
            return stringSplitter.GetAlleleNamesFromAlleleString(mac.Hla);
            return new MolecularAlleleDetails(mac.Hla);
        }
    }
}