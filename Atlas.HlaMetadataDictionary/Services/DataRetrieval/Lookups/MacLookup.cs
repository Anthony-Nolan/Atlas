using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        protected override async Task<IEnumerable<string>> GetAlleleLookupNames(Locus locus, string lookupName, string hlaNomenclatureVersio)
        {
            return await macDictionary.GetHlaFromMac(lookupName);
        }
    }
}