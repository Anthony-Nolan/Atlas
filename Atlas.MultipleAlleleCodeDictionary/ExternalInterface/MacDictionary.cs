using System.Threading.Tasks;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;
using Atlas.MultipleAlleleCodeDictionary.MacCacheService;
using Atlas.MultipleAlleleCodeDictionary.MacImportServices;

namespace Atlas.MultipleAlleleCodeDictionary.ExternalInterface
{
    public interface IMacDictionary
    {
        public Task ImportLatestMacs();
        public Task<Mac> GetMac(string macCode);
        public Task GenerateMacCache();
        public Task<string> GetHlaFromMac(string macCode);
    }

    public class MacDictionary : IMacDictionary
    {
        private readonly IMacImporter macImporter;
        private readonly IMacCacheService macCacheService;

        public MacDictionary(IMacImporter macImporter, IMacCacheService macCacheService)
        {
            this.macImporter = macImporter;
            this.macCacheService = macCacheService;
        }

        public async Task ImportLatestMacs()
        {
            await macImporter.ImportLatestMacs();
        }

        public async Task<Mac> GetMac(string macCode)
        {
            return await macCacheService.GetMacCode(macCode);
        }

        public async Task GenerateMacCache()
        {
            await macCacheService.GenerateMacCache();
        }

        public async Task<string> GetHlaFromMac(string macCode)
        {
            return await macCacheService.GetHlaFromMac(macCode);
        }
    }
}