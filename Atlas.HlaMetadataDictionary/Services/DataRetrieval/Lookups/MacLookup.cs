using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models.MolecularHlaTyping;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;
using Atlas.MultipleAlleleCodeDictionary;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval.Lookups
{
    internal class MacLookup : AlleleNamesLookupBase
    {
        private readonly IMacDictionary macDictionary;

        public MacLookup(
            IHlaMetadataRepository hlaMetadataRepository,
            IAlleleNamesMetadataService alleleNamesMetadataService,
            IMacDictionary macDictionary)
            : base(hlaMetadataRepository, alleleNamesMetadataService)
        {
            this.macDictionary = macDictionary;
        }

        protected override async Task<IEnumerable<string>> GetAlleleLookupNames(Locus locus, string lookupName)
        {
            var parts = lookupName.Split(MolecularTypingNameConstants.FieldDelimiter);
            var firstField = parts[0];
            var mac = parts[1];
            return await macDictionary.GetHlaFromMac(mac, firstField);
        }
    }
}