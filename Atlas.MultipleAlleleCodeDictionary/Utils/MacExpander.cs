using System.Collections.Generic;
using System.Linq;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;
using Atlas.Common.GeneticData.Hla.Models.MolecularHlaTyping;
using Atlas.Common.GeneticData.Hla.Services;
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
                combinedFields[i] = $"{firstField}{FieldDelimiter}{secondFields[i]}";
            }

            return combinedFields.Select(x => new MolecularAlleleDetails(x));
        }

        private static IEnumerable<MolecularAlleleDetails> GetSpecificMac(MultipleAlleleCode mac)
        {
            var alleles = mac.Hla.Split(AlleleDelimiter);
            return alleles.Select(x => new MolecularAlleleDetails(x));
        }
    }
}