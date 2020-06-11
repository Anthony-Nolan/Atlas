using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;

namespace Atlas.MultipleAlleleCodeDictionary.utils
{
    internal interface IMacExpander
    {
        public string GetSpecificMacFromGeneric(MultipleAlleleCode mac);
    }
    
    internal class MacExpander : IMacExpander
    {
        public string GetSpecificMacFromGeneric(MultipleAlleleCode mac)
        {
            throw new System.NotImplementedException();
        }
    }
}