using System.Threading.Tasks;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;
using Atlas.MultipleAlleleCodeDictionary.MacCacheService;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval.Lookups
{

    public interface IMacLookup
    {
        public Task<string> GetHlaFromMac(string mac);
    }
    
    internal class MacLookup : IMacLookup
    {
        private readonly IMultipleAlleleCodeDictionary macDictionary;

        public MacLookup(IMultipleAlleleCodeDictionary macDictionary)
        {
            this.macDictionary = macDictionary;
        }
        
        public async Task<string> GetHlaFromMac(string mac)
        {
            return await macDictionary.GetHlaFromMac(mac);
        }
    }
}