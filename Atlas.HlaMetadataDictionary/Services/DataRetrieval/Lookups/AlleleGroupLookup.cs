using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval.Lookups
{
    internal class AlleleGroupLookup : AlleleNamesLookupBase
    {
        private readonly IAlleleGroupMetadataService alleleGroupMetadataService;

        public AlleleGroupLookup(
            IHlaMetadataRepository hlaMetadataRepository,
            IAlleleNamesMetadataService alleleNamesMetadataService,
            IAlleleGroupMetadataService alleleGroupMetadataService)
            : base(hlaMetadataRepository, alleleNamesMetadataService)
        {
            this.alleleGroupMetadataService = alleleGroupMetadataService;
        }

        protected override async Task<IEnumerable<string>> GetAlleleLookupNames(Locus locus, string lookupName, string hlaNomenclatureVersion)
        {
            return await alleleGroupMetadataService.GetAllelesInGroup(locus, lookupName, hlaNomenclatureVersion);
        }
    }
}