using Atlas.Common.Caching;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.InternalModels.MetadataTableRows;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval.Lookups;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval
{
    internal interface ISearchRelatedMetadataService<THlaMetadata> where THlaMetadata : IHlaMetadata
    {
        Task<THlaMetadata> GetHlaMetadata(Locus locus, string hlaName, string hlaNomenclatureVersion);
    }

    /// <summary>
    /// Common functionality when looking up search-related metadata (e.g., for matching, scoring, MPA, etc.)
    /// where the submitted HLA name could be any supported <see cref="Common.GeneticData.Hla.Models.HlaTypingCategory"/>.
    /// </summary>
    internal abstract class SearchRelatedMetadataServiceBase<THlaMetadata> :
        MetadataServiceBase<THlaMetadata>,
        ISearchRelatedMetadataService<THlaMetadata>
        where THlaMetadata : IHlaMetadata
    {
        protected readonly IHlaCategorisationService HlaCategorisationService;

        private readonly IHlaMetadataRepository hlaMetadataRepository;
        private readonly IAlleleNamesMetadataService alleleNamesMetadataService;
        private readonly IAlleleNamesExtractor alleleNamesExtractor;
        private readonly IMacDictionary macDictionary;
        private readonly IAlleleGroupExpander alleleGroupExpander;
        private readonly ISmallGGroupToPGroupMetadataService smallGGroupToPGroupMetadataService;

        protected SearchRelatedMetadataServiceBase(
            IHlaMetadataRepository hlaMetadataRepository,
            IAlleleNamesMetadataService alleleNamesMetadataService,
            IHlaCategorisationService hlaCategorisationService,
            IAlleleNamesExtractor alleleNamesExtractor,
            IMacDictionary macDictionary,
            IAlleleGroupExpander alleleGroupExpander,
            ISmallGGroupToPGroupMetadataService smallGGroupToPGroupMetadataService,
            string perTypeCacheKey,
            IPersistentCacheProvider cacheProvider)
            : base(perTypeCacheKey, cacheProvider)
        {
            this.hlaMetadataRepository = hlaMetadataRepository;
            this.alleleNamesMetadataService = alleleNamesMetadataService;
            HlaCategorisationService = hlaCategorisationService;
            this.alleleNamesExtractor = alleleNamesExtractor;
            this.macDictionary = macDictionary;
            this.alleleGroupExpander = alleleGroupExpander;
            this.smallGGroupToPGroupMetadataService = smallGGroupToPGroupMetadataService;
        }

        public async Task<THlaMetadata> GetHlaMetadata(Locus locus, string hlaName, string hlaNomenclatureVersion)
        {
            return await GetMetadata(locus, hlaName, hlaNomenclatureVersion);
        }

        protected override bool LookupNameIsValid(string lookupName)
        {
            return !string.IsNullOrEmpty(lookupName);
        }

        protected override async Task<THlaMetadata> PerformLookup(Locus locus, string lookupName, string hlaNomenclatureVersion)
        {
            var dictionaryLookup = GetHlaLookup(lookupName);
            var metadataRows = await dictionaryLookup.PerformLookupAsync(locus, lookupName, hlaNomenclatureVersion);
            var metadata = ConvertMetadataRowsToMetadata(metadataRows).ToList();

            return ConsolidateHlaMetadata(locus, lookupName, metadata);
        }

        private HlaLookupBase GetHlaLookup(string lookupName)
        {
            var hlaTypingCategory = HlaCategorisationService.GetHlaTypingCategory(lookupName);

            return HlaLookupFactory
                .GetLookupByHlaTypingCategory(
                    hlaTypingCategory,
                    hlaMetadataRepository,
                    alleleNamesMetadataService,
                    alleleNamesExtractor,
                    macDictionary,
                    alleleGroupExpander,
                    smallGGroupToPGroupMetadataService);
        }

        protected abstract IEnumerable<THlaMetadata> ConvertMetadataRowsToMetadata(IEnumerable<HlaMetadataTableRow> rows);

        protected abstract THlaMetadata ConsolidateHlaMetadata(Locus locus, string lookupName, List<THlaMetadata> metadata);
    }
}