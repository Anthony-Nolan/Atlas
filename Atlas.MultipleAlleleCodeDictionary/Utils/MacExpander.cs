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
        IEnumerable<MolecularAlleleDetails> ExpandMac(Mac mac, string firstField = null);
    }
    
    internal class MacExpander : IMacExpander
    {
        private const char AlleleDelimiter = '/';
        private const char FieldDelimiter = ':';

        public IEnumerable<MolecularAlleleDetails> ExpandMac(Mac mac, string firstField)
        {
            return mac.IsGeneric ? GetGenericMac(mac, firstField) : GetSpecificMac(mac);
        }
        private static IEnumerable<MolecularAlleleDetails> GetGenericMac(Mac mac, string firstField = null)
        {
            var secondFields = mac.Hla.Split(AlleleDelimiter);
            var combinedFields = new string[secondFields.Length];
            for (var i = 0; i < secondFields.Length; i++)
            {
                combinedFields[i] = $"{firstField}{FieldDelimiter}{secondFields[i]}";
            }

            return combinedFields.Select(x => new MolecularAlleleDetails(x));
        }

        private static IEnumerable<MolecularAlleleDetails> GetSpecificMac(Mac mac)
        {
            var alleles = mac.Hla.Split(AlleleDelimiter);
            return alleles.Select(x => new MolecularAlleleDetails(x));
        }
    }
}