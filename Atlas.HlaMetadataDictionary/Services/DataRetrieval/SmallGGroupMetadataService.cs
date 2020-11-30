using Atlas.Common.Caching;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;
using Atlas.HlaMetadataDictionary.InternalModels.MetadataTableRows;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval
{
    internal interface ISmallGGroupMetadataService : ISearchRelatedMetadataService<ISmallGGroupsMetadata>
    {
        /// <summary>
        ///  Consolidates small g group assignments for a given <paramref name="hlaName"/>.
        /// </summary>
        Task<IEnumerable<string>> GetSmallGGroups(Locus locus, string hlaName, string hlaNomenclatureVersion);
    }

    internal class SmallGGroupMetadataService : 
        SearchRelatedMetadataServiceBase<ISmallGGroupsMetadata>, 
        ISmallGGroupMetadataService
    {
        private const string CacheKey = nameof(SmallGGroupMetadataService);

        public SmallGGroupMetadataService(
            ISmallGGroupsMetadataRepository smallGGroupsMetadataRepository,
            IAlleleNamesMetadataService alleleNamesMetadataService,
            IHlaCategorisationService hlaCategorisationService,
            IAlleleNamesExtractor alleleNamesExtractor,
            IMacDictionary macDictionary,
            IAlleleGroupExpander alleleGroupExpander,
            IPersistentCacheProvider cacheProvider)
            : base(
                smallGGroupsMetadataRepository,
                alleleNamesMetadataService,
                hlaCategorisationService,
                alleleNamesExtractor,
                macDictionary,
                alleleGroupExpander,
                CacheKey,
                cacheProvider)
        {
        }

        public async Task<IEnumerable<string>> GetSmallGGroups(Locus locus,  string hlaName, string hlaNomenclatureVersion)
        {
            var metadata = await GetHlaMetadata(locus, hlaName, hlaNomenclatureVersion);
            return metadata.SmallGGroups;
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
            var groups = metadata
                .SelectMany(data => data.SmallGGroups)
                .Distinct()
                .ToList();

            return new SmallGGroupsMetadata(locus, lookupName, groups);
        }
    }
}