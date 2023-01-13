using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.HlaMetadataDictionary.InternalModels.MetadataTableRows;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Caching;
using Atlas.Common.Public.Models.GeneticData;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval
{
    /// <summary>
    /// Lookup scoring info for each typing that maps to the submitted HLA name.
    /// The relationship of info-to-typing is preserved within the result
    /// for typing categories that require it; else the data is consolidated.
    /// </summary>
    internal interface IHlaScoringMetadataService : ISearchRelatedMetadataService<IHlaScoringMetadata>
    {
        Task<IDictionary<Locus, List<string>>> GetAllGGroups(string hlaNomenclatureVersion);
    }

    internal class HlaScoringMetadataService :
        SearchRelatedMetadataServiceBase<IHlaScoringMetadata>,
        IHlaScoringMetadataService
    {
        private const string CacheKey = nameof(HlaScoringMetadataService);
        private readonly IHlaScoringMetadataRepository hlaScoringMetadataRepository;

        public HlaScoringMetadataService(
            IHlaScoringMetadataRepository hlaScoringMetadataRepository,
            IAlleleNamesMetadataService alleleNamesMetadataService,
            IHlaCategorisationService hlaCategorisationService,
            IAlleleNamesExtractor alleleNamesExtractor,
            IMacDictionary macDictionary,
            IAlleleGroupExpander alleleGroupExpander,
            IPersistentCacheProvider cacheProvider
            ) : base(
                hlaScoringMetadataRepository,
                alleleNamesMetadataService,
                hlaCategorisationService,
                alleleNamesExtractor,
                macDictionary,
                alleleGroupExpander,
                CacheKey,
                cacheProvider)
        {
            this.hlaScoringMetadataRepository = hlaScoringMetadataRepository;
        }
        
        public async Task<IDictionary<Locus, List<string>>> GetAllGGroups(string hlaNomenclatureVersion)
        {
            return await hlaScoringMetadataRepository.GetAllGGroups(hlaNomenclatureVersion);
        }

        protected override IEnumerable<IHlaScoringMetadata> ConvertMetadataRowsToMetadata(IEnumerable<HlaMetadataTableRow> rows)
        {
            return rows.Select(row => row.ToHlaScoringMetadata());
        }

        protected override IHlaScoringMetadata ConsolidateHlaMetadata(
            Locus locus,
            string lookupName,
            List<IHlaScoringMetadata> metadata)
        {
            var hlaTypingCategory = HlaCategorisationService.GetHlaTypingCategory(lookupName);
            var scoringInfos = metadata.Select(result => result.HlaScoringInfo).ToList();

            switch (hlaTypingCategory)
            {
                case HlaTypingCategory.Allele:
                    return metadata.Count == 1
                        ? metadata.Single()
                        : GetMultipleAlleleMetadata(locus, lookupName, scoringInfos);

                // TODO: ATLAS-454 - Confirm lookup strategy for scoring of P/G/small g group HLA.
                // I.e., return each allele's metadata or consolidated metadata.

                case HlaTypingCategory.PGroup:
                case HlaTypingCategory.GGroup:
                case HlaTypingCategory.SmallGGroup:
                case HlaTypingCategory.AlleleStringOfNames:
                case HlaTypingCategory.AlleleStringOfSubtypes:
                case HlaTypingCategory.NmdpCode:
                    return GetConsolidatedMolecularMetadata(locus, lookupName, scoringInfos);

                case HlaTypingCategory.XxCode:
                case HlaTypingCategory.Serology:
                    return metadata.Single();

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static IHlaScoringMetadata GetMultipleAlleleMetadata(
            Locus locus,
            string lookupName,
            IReadOnlyCollection<IHlaScoringInfo> scoringInfos)
        {
            var alleleScoringInfos = GetSingleAlleleScoringInfos(scoringInfos).ToList();
            var matchingSerologies = GetMatchingSerologies(scoringInfos);

            var multipleAlleleScoringInfo = new MultipleAlleleScoringInfo(
                alleleScoringInfos,
                matchingSerologies);

            return new HlaScoringMetadata(
                locus,
                lookupName,
                multipleAlleleScoringInfo,
                TypingMethod.Molecular
            );
        }

        private static IHlaScoringMetadata GetConsolidatedMolecularMetadata(
            Locus locus,
            string lookupName,
            IReadOnlyCollection<IHlaScoringInfo> scoringInfos)
        {
            var singleAlleleScoringInfos = GetSingleAlleleScoringInfos(scoringInfos).ToList();

            var matchingPGroups = GetMatchingPGroups(singleAlleleScoringInfos);
            var matchingGGroups = GetMatchingGGroups(singleAlleleScoringInfos);
            var matchingSerologies = GetMatchingSerologies(scoringInfos);

            var consolidatedMolecularScoringInfo = new ConsolidatedMolecularScoringInfo(
                matchingPGroups,
                matchingGGroups,
                matchingSerologies);

            return new HlaScoringMetadata(
                locus,
                lookupName,
                consolidatedMolecularScoringInfo,
                TypingMethod.Molecular
            );
        }

        private static IEnumerable<SingleAlleleScoringInfo> GetSingleAlleleScoringInfos(IEnumerable<IHlaScoringInfo> scoringInfos)
        {
            var infos = scoringInfos.ToList();

            var singleAlleleInfos = infos.OfType<SingleAlleleScoringInfo>()
                .Union(infos.OfType<MultipleAlleleScoringInfo>()
                    .SelectMany(multiple => multiple.AlleleScoringInfos));

            return MultipleAlleleNullFilter.Filter(singleAlleleInfos);
        }

        private static IEnumerable<string> GetMatchingPGroups(IEnumerable<SingleAlleleScoringInfo> singleAlleleScoringInfos)
        {
            return singleAlleleScoringInfos
                .Select(single => single.MatchingPGroup)
                .Distinct();
        }

        private static IEnumerable<string> GetMatchingGGroups(IEnumerable<SingleAlleleScoringInfo> singleAlleleScoringInfos)
        {
            return singleAlleleScoringInfos
                .Select(single => single.MatchingGGroup)
                .Distinct();
        }

        private static IEnumerable<SerologyEntry> GetMatchingSerologies(IEnumerable<IHlaScoringInfo> scoringInfos)
        {
            return scoringInfos
                .SelectMany(info => info.MatchingSerologies)
                .Distinct();
        }
    }
}