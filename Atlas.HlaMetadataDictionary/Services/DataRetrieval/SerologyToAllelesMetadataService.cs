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
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval
{
    internal interface ISerologyToAllelesMetadataService : ISearchRelatedMetadataService<ISerologyToAllelesMetadata>
    {
        Task<IEnumerable<SerologyToAlleleMappingSummary>> GetSerologyToAlleleMappings(
            Locus locus, string serologyName, string hlaNomenclatureVersion);
    }

    internal class SerologyToAllelesMetadataService :
        SearchRelatedMetadataServiceBase<ISerologyToAllelesMetadata>,
        ISerologyToAllelesMetadataService
    {
        private const string CacheKey = nameof(SerologyToAllelesMetadataService);

        public SerologyToAllelesMetadataService(
            // ReSharper disable once SuggestBaseTypeForParameterInConstructor
            ISerologyToAllelesMetadataRepository serologyToAllelesMetadataRepository,
            IAlleleNamesMetadataService alleleNamesMetadataService,
            IHlaCategorisationService hlaCategorisationService,
            IAlleleNamesExtractor alleleNamesExtractor,
            IMacDictionary macDictionary,
            IAlleleGroupExpander alleleGroupExpander,
            IPersistentCacheProvider cacheProvider,
            HlaMetadataDictionarySettings options)
            : base(
                serologyToAllelesMetadataRepository,
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
        }

        public async Task<IEnumerable<SerologyToAlleleMappingSummary>> GetSerologyToAlleleMappings(Locus locus, string serologyName,
            string hlaNomenclatureVersion)
        {
            var metadata = await GetHlaMetadata(locus, serologyName, hlaNomenclatureVersion);
            return metadata.SerologyToAlleleMappings;
        }

        protected override IEnumerable<ISerologyToAllelesMetadata> ConvertMetadataRowsToMetadata(
            IEnumerable<HlaMetadataTableRow> rows)
        {
            return rows.Select(row => row.ToSerologyToAlleleMetadata());
        }

        protected override ISerologyToAllelesMetadata ConsolidateHlaMetadata(
            Locus locus,
            string lookupName,
            List<ISerologyToAllelesMetadata> metadata)
        {
            // should only be 1 metadata row per valid serology typing
            // No rows is a name with no data, and has to say so: GetMetadata no longer re-labels the Single()
            // failure as one. See HlaScoringMetadataService.ConsolidateHlaMetadata.
            if (metadata.Count == 0)
            {
                throw new InvalidHlaException(locus, lookupName);
            }

            return metadata.Single();
        }
    }
}