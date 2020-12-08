using Atlas.Common.GeneticData.Hla.Services;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;
using System.Collections.Generic;
using Atlas.Common.GeneticData.Hla.Services.AlleleNameUtils;

namespace Atlas.MultipleAlleleCodeDictionary.Services
{
    internal interface IMacExpander
    {
        /// <remarks>
        /// This class makes no guarantees about the validity of any produced HLA.
        ///
        /// Generic MACs have the potential to lead to invalid alleles for some first fields.
        /// Specific MACs ignore the given first field - meaning some technically invalid MACs will be expanded to valid alleles. 
        /// </remarks>
        IEnumerable<string> ExpandMac(Mac mac, string firstField = null);
    }
    
    internal class MacExpander : IMacExpander
    {
        /// <inheritdoc />
        public IEnumerable<string> ExpandMac(Mac mac, string firstField)
        {
            return mac.IsGeneric ? ExpandGenericMac(mac, firstField) : ExpandSpecificMac(mac);
        }
        
        /// <summary>
        /// This method can produce invalid alleles, for incompatible first/second field combinations.
        /// </summary>
        private static IEnumerable<string> ExpandGenericMac(Mac mac, string firstField = null)
        {
            return AlleleStringSplitter.SplitAlleleStringOfSubtypesToAlleleNames($"{firstField}:" + mac.Hla);
        }

        /// <remarks>
        /// This method cannot produce invalid alleles.
        /// Note that it does not accept a first field, and will always produce the same alleles for a given MAC.
        /// </remarks>
        private static IEnumerable<string> ExpandSpecificMac(Mac mac)
        {
            return AlleleStringSplitter.SplitAlleleString(mac.Hla);
        }
    }
}