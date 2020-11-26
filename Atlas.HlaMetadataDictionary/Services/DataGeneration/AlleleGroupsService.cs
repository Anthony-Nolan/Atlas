using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.HlaMetadataDictionary.HlaTypingInfo;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;
using Atlas.HlaMetadataDictionary.Repositories;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;
using System.Collections.Generic;
using System.Linq;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration
{
    internal interface IAlleleGroupsService
    {
        /// <summary>
        /// Retrieves metadata for P and G groups from the HLA nomenclature repository.
        /// Note: Excludes single alleles listed in the hla_nom_p/g files, that do not map to an allele group.
        /// </summary>
        IEnumerable<IAlleleGroupMetadata> GetAlleleGroupsMetadata(string hlaNomenclatureVersion);
    }

    internal class AlleleGroupsService : IAlleleGroupsService
    {
        private readonly IWmdaDataRepository wmdaDataRepository;
        private readonly IHlaCategorisationService hlaCategorisationService;

        public AlleleGroupsService(IWmdaDataRepository wmdaDataRepository, IHlaCategorisationService hlaCategorisationService)
        {
            this.wmdaDataRepository = wmdaDataRepository;
            this.hlaCategorisationService = hlaCategorisationService;
        }

        public IEnumerable<IAlleleGroupMetadata> GetAlleleGroupsMetadata(string hlaNomenclatureVersion)
        {
            var dataset = wmdaDataRepository.GetWmdaDataset(hlaNomenclatureVersion);
            var pGroups = dataset.PGroups.Select(GetMetadataFromAlleleGroup);
            var gGroups = dataset.GGroups.Select(GetMetadataFromAlleleGroup);
            
            return pGroups.Concat(gGroups).Where(IsAlleleGroup);
        }

        private static IAlleleGroupMetadata GetMetadataFromAlleleGroup(IWmdaAlleleGroup alleleGroup)
        {
            var locus = HlaMetadataDictionaryLoci.GetLocusFromTypingLocusNameIfExists(TypingMethod.Molecular, alleleGroup.TypingLocus);
            return new AlleleGroupMetadata(locus, alleleGroup.Name, alleleGroup.Alleles);
        }

        private bool IsAlleleGroup(IAlleleGroupMetadata metadata)
        {
            var category = hlaCategorisationService.GetHlaTypingCategory(metadata.LookupName);
            return category == HlaTypingCategory.GGroup || category == HlaTypingCategory.PGroup;
        }
    }
}
