using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.HlaMetadataDictionary.InternalExceptions;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval
{
    internal interface IAlleleGroupMetadataService
    {
        Task<IEnumerable<string>> GetAllelesInGroup(Locus locus, string alleleGroupLookupName, string hlaNomenclatureVersion);
    }

    internal class AlleleGroupMetadataService : MetadataServiceBase<IEnumerable<string>>, IAlleleGroupMetadataService
    {
        private readonly IAlleleGroupsMetadataRepository alleleGroupsMetadataRepository;
        private readonly IHlaCategorisationService hlaCategorisationService;

        public AlleleGroupMetadataService(
            IAlleleGroupsMetadataRepository alleleGroupsMetadataRepository, 
            IHlaCategorisationService hlaCategorisationService)
        {
            this.alleleGroupsMetadataRepository = alleleGroupsMetadataRepository;
            this.hlaCategorisationService = hlaCategorisationService;
        }

        public async Task<IEnumerable<string>> GetAllelesInGroup(Locus locus, string alleleGroupLookupName, string hlaNomenclatureVersion)
        {
            return await GetMetadata(locus, alleleGroupLookupName, hlaNomenclatureVersion);
        }

        protected override bool LookupNameIsValid(string lookupName)
        {
            return !string.IsNullOrEmpty(lookupName) && IsAlleleGroup(lookupName);
        }

        protected override async Task<IEnumerable<string>> PerformLookup(Locus locus, string lookupName, string hlaNomenclatureVersion)
        {
            var metadata = await alleleGroupsMetadataRepository.GetAlleleGroupIfExists(locus, lookupName, hlaNomenclatureVersion);

            if (metadata == null)
            {
                throw new InvalidHlaException(locus, lookupName);
            }

            return metadata.AllelesInGroup;
        }

        private bool IsAlleleGroup(string lookupName)
        {
            var category = hlaCategorisationService.GetHlaTypingCategory(lookupName);
            return category == HlaTypingCategory.GGroup || category == HlaTypingCategory.PGroup;
        }
    }
}