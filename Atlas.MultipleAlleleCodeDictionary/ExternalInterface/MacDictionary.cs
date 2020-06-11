using System.Threading.Tasks;
using Atlas.Common.GeneticData.Hla.Models.MolecularHlaTyping;
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
         public Task<MolecularAlleleDetails> GetHlaFromMac(string macCode, string firstField);
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

        public async Task<MolecularAlleleDetails> GetHlaFromMac(string macCode, string firstField)
        {
            return await macCache.GetHlaFromMac(macCode, firstField);
        }
    }
}