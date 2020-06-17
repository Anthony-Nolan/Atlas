using System.Collections.Generic;
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
        /// <remarks>
        /// This does not guarantee that HLA generated will be valid.
        /// For instance, a generic MAC might not be valid for a given first field.
        /// </remarks>
        public Task<IEnumerable<string>> GetHlaFromMac(string macCode, string firstField);
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

        /// <inheritdoc />
        public async Task<IEnumerable<string>> GetHlaFromMac(string macCode, string firstField)
        {
            return await macCacheService.GetHlaFromMac(macCode, firstField);
        }
    }
}