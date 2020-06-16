using System.Collections.Generic;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;
using Atlas.Common.GeneticData.Hla.Models.MolecularHlaTyping;
using Atlas.Common.GeneticData.Hla.Services.AlleleStringSplitters;

namespace Atlas.MultipleAlleleCodeDictionary.utils
{
    internal interface IMacExpander
    {
        IEnumerable<MolecularAlleleDetails> ExpandMac(MultipleAlleleCode mac, string firstField);
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
        
        public IEnumerable<MolecularAlleleDetails> ExpandMac(MultipleAlleleCode mac, string firstField)
        {
            return mac.IsGeneric ? GetGenericMac(mac, firstField) : GetSpecificMac(mac);
        }
        private static IEnumerable<MolecularAlleleDetails> GetGenericMac(MultipleAlleleCode mac, string firstField)
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

        private static IEnumerable<MolecularAlleleDetails> GetSpecificMac(MultipleAlleleCode mac)
        {
            return new List<MolecularAlleleDetails>()
            {
                new MolecularAlleleDetails(mac.Hla)
            };
        }
    }
}