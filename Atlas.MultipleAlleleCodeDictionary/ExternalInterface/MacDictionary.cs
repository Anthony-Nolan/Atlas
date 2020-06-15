using System.Threading.Tasks;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;
using Atlas.MultipleAlleleCodeDictionary.MacCacheService;
using Atlas.MultipleAlleleCodeDictionary.MacImportServices;

namespace Atlas.MultipleAlleleCodeDictionary.ExternalInterface
{
    public interface IMacDictionary
    {
        /// <summary>
        /// Collects the most recent MAC data from the external source provided in MacImportSettings,
        /// and then stores it in the provided azure storage account.
        /// </summary>
        public Task ImportLatestMacs();
        /// <summary>
        /// Fetch the HLA for a given MAC from the storage account, caching appropriately.
        /// </summary>
        public Task<Mac> GetMac(string macCode);
        /// <summary>
        /// A debug endpoint to regenerate the MAC Cache.
        /// </summary>
        public Task GenerateMacCache();
    }
    
    public class MacDictionary : IMacDictionary
    {
        private readonly IMacImporter macImporter;
        private readonly IMacCache macCache;

        public MacDictionary(IMacImporter macImporter, IMacCache macCache)
        {
            this.macImporter = macImporter;
            this.macCache = macCache;
        }
        
        public async Task ImportLatestMacs()
        {
            await macImporter.ImportLatestMacs();
        }
        
        public async Task<Mac> GetMac(string macCode)
        {
            return await macCache.GetMacCode(macCode);
        }
        
        public async Task GenerateMacCache()
        {
            await macCache.GenerateMacCache();
        }
        
    }
}