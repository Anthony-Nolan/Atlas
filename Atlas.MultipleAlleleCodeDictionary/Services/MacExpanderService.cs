using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.Hla.Models.MolecularHlaTyping;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;

namespace Atlas.MultipleAlleleCodeDictionary.Services
{
    internal interface IMacExpander
    {
        /// <remarks>
        /// This class makes no guarantees about the validity of any produced HLA
        /// </remarks>
        IEnumerable<string> ExpandMac(Mac mac, string firstField = null);
    }
    
    internal class MacExpander : IMacExpander
    {
        private const char AlleleDelimiter = '/';

        /// <inheritdoc />
        public IEnumerable<string> ExpandMac(Mac mac, string firstField)
        {
            return mac.IsGeneric ? ExpandGenericMac(mac, firstField) : ExpandSpecificMac(mac);
        }
        
        private static IEnumerable<string> ExpandGenericMac(Mac mac, string firstField = null)
        {
            var secondFields = mac.Hla.Split(AlleleDelimiter);
            return secondFields.Select(secondField =>
                new MolecularAlleleDetails(firstField, secondField).AlleleNameWithoutPrefix);
        }

        /// <remarks>
        /// This method can produce multiple invalid alleles, particularly in a case where a given first field does not
        /// match any of the specific alleles for that MAC 
        /// </remarks>
        private static IEnumerable<string> ExpandSpecificMac(Mac mac)
        {
            return mac.Hla.Split(AlleleDelimiter);
        }
    }
}