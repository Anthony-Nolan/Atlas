using Atlas.Common.Caching;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.HlaMetadataDictionary.InternalExceptions;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval
{
    internal interface IAlleleGroupExpander
    {
        /// <returns>All alleles represented by the provided P- or G-group.</returns>
        Task<IEnumerable<string>> ExpandAlleleGroup(Locus locus, string alleleGroup, string hlaNomenclatureVersion);
    }

    internal class AlleleGroupExpander : MetadataServiceBase<IEnumerable<string>>, IAlleleGroupExpander
    {
        private const string CacheKey = nameof(AlleleGroupExpander);

        private readonly IAlleleGroupsMetadataRepository alleleGroupsMetadataRepository;
        private readonly IHlaCategorisationService hlaCategorisationService;

        public AlleleGroupExpander(
            IAlleleGroupsMetadataRepository alleleGroupsMetadataRepository,
            IHlaCategorisationService hlaCategorisationService,
            IPersistentCacheProvider cacheProvider)
        : base(CacheKey, cacheProvider)
        {
            this.alleleGroupsMetadataRepository = alleleGroupsMetadataRepository;
            this.hlaCategorisationService = hlaCategorisationService;
        }

        public async Task<IEnumerable<string>> ExpandAlleleGroup(Locus locus, string alleleGroup, string hlaNomenclatureVersion)
        {
            return await GetMetadata(locus, alleleGroup, hlaNomenclatureVersion);
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

            return metadata.AllelesInGroup.ToList();
        }

        private bool IsAlleleGroup(string lookupName)
        {
            var category = hlaCategorisationService.GetHlaTypingCategory(lookupName);
            return new[]{HlaTypingCategory.GGroup, HlaTypingCategory.PGroup, HlaTypingCategory.SmallGGroup}.Contains(category);
        }
    }
}