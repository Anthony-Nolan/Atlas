using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval.Lookups
{
    internal class AlleleStringLookup : AlleleNamesLookupBase
    {
        private readonly IAlleleStringSplitterService alleleSplitter;
        
        public AlleleStringLookup(
            IHlaMetadataRepository hlaMetadataRepository,
            IAlleleNamesMetadataService alleleNamesMetadataService,
            IAlleleStringSplitterService alleleSplitter)
            : base(hlaMetadataRepository, alleleNamesMetadataService)
        {
            this.alleleSplitter = alleleSplitter;
        }

        protected override async Task<IEnumerable<string>> GetAlleleLookupNames(Locus locus, string lookupName, string hlaNomenclatureVersion)
        {
            return await Task.Run(() => alleleSplitter.GetAlleleNamesFromAlleleString(lookupName));
        }
    }
}
