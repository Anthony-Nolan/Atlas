using System.Threading.Tasks;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;
using Atlas.MultipleAlleleCodeDictionary.MacCacheService;
using Atlas.MultipleAlleleCodeDictionary.MacImportServices;

namespace Atlas.MultipleAlleleCodeDictionary.ExternalInterface
{
    public interface IMultipleAlleleCodeDictionary
    {
        public Task ImportLatestMultipleAlleleCodes();
        public Task<MultipleAlleleCode> GetMultipleAlleleCode(string macCode);
        public Task GenerateMacCache();
    }
    
    public class MultipleAlleleCodeDictionary : IMultipleAlleleCodeDictionary
    {
        private readonly IMacImporter macImporter;
        private readonly IMacCache macCache;

        public MultipleAlleleCodeDictionary(IMacImporter macImporter, IMacCache macCache)
        {
            this.macImporter = macImporter;
            this.macCache = macCache;
        }
        
        public async Task ImportLatestMultipleAlleleCodes()
        {
            await macImporter.ImportLatestMultipleAlleleCodes();
        }
        
        public async Task<MultipleAlleleCode> GetMultipleAlleleCode(string macCode)
        {
            return await macCache.GetMacCode(macCode);
        }
        
        public async Task GenerateMacCache()
        {
            await macCache.GenerateMacCache();
        }
        
    }
}