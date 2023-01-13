using Atlas.Common.Caching;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.InternalModels.MetadataTableRows;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval
{
    /// <summary>
    ///  Consolidates HLA info used in matching for all alleles that map to the hla name.
    /// </summary>
    internal interface IHlaMatchingMetadataService : ISearchRelatedMetadataService<IHlaMatchingMetadata>
    {
        Task<IEnumerable<string>> GetAllPGroups(string hlaNomenclatureVersion);
    }

    internal class HlaMatchingMetadataService : 
        SearchRelatedMetadataServiceBase<IHlaMatchingMetadata>, 
        IHlaMatchingMetadataService
    {
        private const string CacheKey = nameof(HlaMatchingMetadataService);
        private readonly IHlaMatchingMetadataRepository typedMatchingRepository;

        public HlaMatchingMetadataService(
            IHlaMatchingMetadataRepository hlaMatchingMetadataRepository,
            IAlleleNamesMetadataService alleleNamesMetadataService,
            IHlaCategorisationService hlaCategorisationService,
            IAlleleNamesExtractor alleleNamesExtractor,
            IMacDictionary macDictionary,
            IAlleleGroupExpander alleleGroupExpander,
            IPersistentCacheProvider cacheProvider)
            : base(
                hlaMatchingMetadataRepository,
                alleleNamesMetadataService,
                hlaCategorisationService,
                alleleNamesExtractor,
                macDictionary,
                alleleGroupExpander,
                CacheKey,
                cacheProvider)
        {
            typedMatchingRepository = hlaMatchingMetadataRepository;
        }

        protected override IEnumerable<IHlaMatchingMetadata> ConvertMetadataRowsToMetadata(
            IEnumerable<HlaMetadataTableRow> rows)
        {
            return rows.Select(row => row.ToHlaMatchingMetadata());
        }

        protected override IHlaMatchingMetadata ConsolidateHlaMetadata(
            Locus locus,
            string lookupName,
            List<IHlaMatchingMetadata> metadata)
        {
            var typingMethod = metadata
                .First()
                .TypingMethod;

            var pGroups = metadata
                .SelectMany(data => data.MatchingPGroups)
                .Distinct()
                .ToList();

            return new HlaMatchingMetadata(
                locus,
                lookupName,
                typingMethod,
                pGroups);
        }

        public async Task<IEnumerable<string>> GetAllPGroups(string hlaNomenclatureVersion)
        {
            return await typedMatchingRepository.GetAllPGroups(hlaNomenclatureVersion);
        }
    }
}