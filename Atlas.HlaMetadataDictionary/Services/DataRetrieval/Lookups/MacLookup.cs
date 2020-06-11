using System.Threading.Tasks;
using Atlas.Common.GeneticData.Hla.Models.MolecularHlaTyping;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;
using Atlas.MultipleAlleleCodeDictionary.MacCacheService;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval.Lookups
{

    public interface IMacLookup
    {
        public Task<MolecularAlleleDetails> GetHlaFromMac(string mac, string firstField);
    }
    
    internal class MacLookup : IMacLookup
    {
        private readonly IMultipleAlleleCodeDictionary macDictionary;

        public MacLookup(IMultipleAlleleCodeDictionary macDictionary)
        {
            this.macDictionary = macDictionary;
        }
        
        public async Task<MolecularAlleleDetails> GetHlaFromMac(string mac, string firstField)
        {
            return await macDictionary.GetHlaFromMac(mac, firstField);
        }
    }
}