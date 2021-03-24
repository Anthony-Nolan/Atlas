using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.HlaMetadataDictionary.HlaTypingInfo;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;
using Atlas.HlaMetadataDictionary.Repositories;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration.Generators
{
    internal interface IAlleleGroupsService
    {
        /// <summary>
        /// Retrieves metadata for P, G and small g groups from the HLA nomenclature repository.
        /// Note: Excludes single alleles that do not map to an allele group.
        /// </summary>
        IEnumerable<IAlleleGroupMetadata> GetAlleleGroupsMetadata(string hlaNomenclatureVersion);
    }

    internal class AlleleGroupsService : IAlleleGroupsService
    {
        private readonly IWmdaDataRepository wmdaDataRepository;
        private readonly ISmallGGroupsBuilder smallGGroupsBuilder;
        private readonly IHlaCategorisationService hlaCategorisationService;

        public AlleleGroupsService(
            IWmdaDataRepository wmdaDataRepository,
            ISmallGGroupsBuilder smallGGroupsBuilder,
            IHlaCategorisationService hlaCategorisationService)
        {
            this.wmdaDataRepository = wmdaDataRepository;
            this.smallGGroupsBuilder = smallGGroupsBuilder;
            this.hlaCategorisationService = hlaCategorisationService;
        }

        public IEnumerable<IAlleleGroupMetadata> GetAlleleGroupsMetadata(string hlaNomenclatureVersion)
        {
            var dataset = wmdaDataRepository.GetWmdaDataset(hlaNomenclatureVersion);
            var pGroups = dataset.PGroups.Select(GetMetadataFromAlleleGroup);
            var gGroups = dataset.GGroups.Select(GetMetadataFromAlleleGroup);
            var smallGGroups = smallGGroupsBuilder.BuildSmallGGroups(hlaNomenclatureVersion)
                .Select(g => new AlleleGroupMetadata(g.Locus, g.Name, g.Alleles));

            return pGroups.Concat(gGroups).Concat(smallGGroups).Where(IsAlleleGroup);
        }

        private static IAlleleGroupMetadata GetMetadataFromAlleleGroup(IWmdaAlleleGroup alleleGroup)
        {
            var locus = HlaMetadataDictionaryLoci.GetLocusFromTypingLocusNameIfExists(TypingMethod.Molecular, alleleGroup.TypingLocus);
            return new AlleleGroupMetadata(locus, alleleGroup.Name, alleleGroup.Alleles);
        }

        private bool IsAlleleGroup(IAlleleGroupMetadata metadata)
        {
            var category = hlaCategorisationService.GetHlaTypingCategory(metadata.LookupName);
            return new[]{HlaTypingCategory.GGroup, HlaTypingCategory.PGroup, HlaTypingCategory.SmallGGroup}.Contains(category);
        }
    }
}
