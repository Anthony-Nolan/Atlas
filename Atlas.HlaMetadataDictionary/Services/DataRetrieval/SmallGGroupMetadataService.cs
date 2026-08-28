using Atlas.Common.Caching;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.HlaMetadataDictionary.InternalExceptions;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;
using Atlas.HlaMetadataDictionary.InternalModels.MetadataTableRows;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval
{
    internal interface ISmallGGroupMetadataService : ISearchRelatedMetadataService<ISmallGGroupsMetadata>
    {
        /// <summary>
        ///  Consolidates small g group assignments for a given <paramref name="hlaName"/>.
        /// </summary>
        Task<IEnumerable<string>> GetSmallGGroups(Locus locus, string hlaName, string hlaNomenclatureVersion);

        /// <summary>
        /// <see cref="GetSmallGGroups"/> for a caller that treats an unknown HLA name as an answer
        /// </summary>
        Task<(bool WasFound, IEnumerable<string> SmallGGroups)> TryGetSmallGGroups(Locus locus, string hlaName, string hlaNomenclatureVersion);

        Task<IDictionary<Locus, ISet<string>>> GetAllSmallGGroups(string hlaNomenclatureVersion);
    }

    internal class SmallGGroupMetadataService :
        SearchRelatedMetadataServiceBase<ISmallGGroupsMetadata>,
        ISmallGGroupMetadataService
    {
        private readonly ISmallGGroupToPGroupMetadataRepository smallGGroupToPGroupMetadataRepository;
        private const string CacheKey = nameof(SmallGGroupMetadataService);
        private const string NewAllele = "NEW";

        public SmallGGroupMetadataService(
            IHlaNameToSmallGGroupLookupRepository hlaNameToSmallGGroupLookupRepository,
            ISmallGGroupToPGroupMetadataRepository smallGGroupToPGroupMetadataRepository,
            IAlleleNamesMetadataService alleleNamesMetadataService,
            IHlaCategorisationService hlaCategorisationService,
            IAlleleNamesExtractor alleleNamesExtractor,
            IMacDictionary macDictionary,
            IAlleleGroupExpander alleleGroupExpander,
            IPersistentCacheProvider cacheProvider,
            HlaMetadataDictionarySettings options)
            : base(
                hlaNameToSmallGGroupLookupRepository,
                alleleNamesMetadataService,
                hlaCategorisationService,
                alleleNamesExtractor,
                macDictionary,
                alleleGroupExpander,
                CacheKey,
                cacheProvider,
                options
                )
        {
            this.smallGGroupToPGroupMetadataRepository = smallGGroupToPGroupMetadataRepository;
        }

        public async Task<IEnumerable<string>> GetSmallGGroups(Locus locus, string hlaName, string hlaNomenclatureVersion)
        {
            if (hlaName == NewAllele)
            {
                return new List<string>();
            }
            var metadata = await GetHlaMetadata(locus, hlaName, hlaNomenclatureVersion);
            return metadata.SmallGGroups;
        }

        /// <inheritdoc />
        public async Task<(bool WasFound, IEnumerable<string> SmallGGroups)> TryGetSmallGGroups(
            Locus locus,
            string hlaName,
            string hlaNomenclatureVersion)
        {
            if (hlaName == NewAllele)
            {
                return (true, new List<string>());
            }

            var (wasFound, metadata) = await TryGetHlaMetadata(locus, hlaName, hlaNomenclatureVersion);

            return (wasFound, wasFound ? metadata.SmallGGroups : null);
        }

        public async Task<IDictionary<Locus, ISet<string>>> GetAllSmallGGroups(string hlaNomenclatureVersion)
        {
            return await smallGGroupToPGroupMetadataRepository.GetAllSmallGGroups(hlaNomenclatureVersion);
        }

        protected override IEnumerable<ISmallGGroupsMetadata> ConvertMetadataRowsToMetadata(
            IEnumerable<HlaMetadataTableRow> rows)
        {
            return rows.Select(row => row.ToSmallGGroupMetadata());
        }

        protected override ISmallGGroupsMetadata ConsolidateHlaMetadata(
            Locus locus,
            string lookupName,
            List<ISmallGGroupsMetadata> metadata)
        {
            // No rows is a name with no data, and has to say so: GetMetadata no longer re-labels the Single()
            // failure as one. See HlaScoringMetadataService.ConsolidateHlaMetadata.
            if (metadata.Count == 0)
            {
                throw new InvalidHlaException(locus, lookupName);
            }

            var typingMethod = metadata.Select(m => m.TypingMethod).Distinct().Single();

            var groups = metadata
                .SelectMany(data => data.SmallGGroups)
                .Distinct()
                .ToList();

            return new SmallGGroupsMetadata(locus, lookupName, typingMethod, groups);
        }
    }
}