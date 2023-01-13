using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;

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

        protected override async Task<List<string>> GetAlleleLookupNames(Locus locus, string lookupName, string hlaNomenclatureVersion)
        {
            return (await macDictionary.GetHlaFromMac(lookupName)).ToList();
        }
    }
}