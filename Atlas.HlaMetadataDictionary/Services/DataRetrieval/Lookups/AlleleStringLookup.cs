using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval.Lookups
{
    internal class AlleleStringLookup : AlleleNamesLookupBase
    {
        private readonly IAlleleNamesExtractor alleleNamesExtractor;
        
        public AlleleStringLookup(
            IHlaMetadataRepository hlaMetadataRepository,
            IAlleleNamesMetadataService alleleNamesMetadataService,
            IAlleleNamesExtractor alleleNamesExtractor)
            : base(hlaMetadataRepository, alleleNamesMetadataService)
        {
            this.alleleNamesExtractor = alleleNamesExtractor;
        }

        protected override async Task<List<string>> GetAlleleLookupNames(Locus locus, string lookupName, string hlaNomenclatureVersion)
        {
            return await Task.Run(() => alleleNamesExtractor.GetAlleleNamesFromAlleleString(lookupName).ToList());
        }
    }
}
