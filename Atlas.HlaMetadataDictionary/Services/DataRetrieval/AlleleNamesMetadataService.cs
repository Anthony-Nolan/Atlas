using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Caching;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.InternalExceptions;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval
{
    internal interface IAlleleNamesMetadataService
    {
        Task<IEnumerable<string>> GetCurrentAlleleNames(Locus locus, string alleleLookupName, string hlaNomenclatureVersion);
    }

    internal class AlleleNamesMetadataService : MetadataServiceBase<IEnumerable<string>>, IAlleleNamesMetadataService
    {
        private const string CacheKey = nameof(AlleleNamesMetadataService);

        private readonly IAlleleNamesMetadataRepository alleleNamesMetadataRepository;
        private readonly IHlaCategorisationService hlaCategorisationService;

        public AlleleNamesMetadataService(
            IAlleleNamesMetadataRepository alleleNamesMetadataRepository, 
            IHlaCategorisationService hlaCategorisationService,
            IPersistentCacheProvider cacheProvider)
            : base(CacheKey, cacheProvider)
        {
            this.alleleNamesMetadataRepository = alleleNamesMetadataRepository;
            this.hlaCategorisationService = hlaCategorisationService;
        }

        public async Task<IEnumerable<string>> GetCurrentAlleleNames(Locus locus, string alleleLookupName, string hlaNomenclatureVersion)
        {
            return await GetMetadata(locus, alleleLookupName, hlaNomenclatureVersion);
        }

        protected override bool LookupNameIsValid(string lookupName)
        {
            return !string.IsNullOrEmpty(lookupName) &&
                   hlaCategorisationService.GetHlaTypingCategory(lookupName) == HlaTypingCategory.Allele;
        }

        protected override async Task<IEnumerable<string>> PerformLookup(Locus locus, string lookupName, string hlaNomenclatureVersion)
        {
            var alleleNameMetadata = await alleleNamesMetadataRepository.GetAlleleNameIfExists(locus, lookupName, hlaNomenclatureVersion);

            if (alleleNameMetadata == null)
            {
                throw new InvalidHlaException(locus, lookupName);
            }

            return alleleNameMetadata.CurrentAlleleNames.ToList();
        }
    }
}