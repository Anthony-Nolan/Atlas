using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.HlaMetadataDictionary.Models.LookupEntities;
using Atlas.HlaMetadataDictionary.Models.Lookups;
using Atlas.HlaMetadataDictionary.Models.Lookups.ScoringLookup;
using Atlas.HlaMetadataDictionary.Repositories.LookupRepositories;
using Atlas.MultipleAlleleCodeDictionary;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval
{
    /// <summary>
    /// Lookup scoring info for each typing that maps to the submitted HLA name.
    /// The relationship of info-to-typing is preserved within the result
    /// for typing categories that require it; else the data is consolidated.
    /// </summary>
    internal interface IHlaScoringMetadataService : IHlaSearchingMetadataService<IHlaScoringMetadata>
    {
    }

    internal class HlaScoringMetadataService :
        HlaSearchingMetadataServiceBase<IHlaScoringMetadata>,
        IHlaScoringMetadataService
    {
        public HlaScoringMetadataService(
            IHlaScoringMetadataRepository hlaScoringMetadataRepository,
            IAlleleNamesMetadataService alleleNamesMetadataService,
            IHlaCategorisationService hlaCategorisationService,
            IAlleleStringSplitterService alleleSplitter,
            INmdpCodeCache cache
        ) : base(
            hlaScoringMetadataRepository,
            alleleNamesMetadataService,
            hlaCategorisationService,
            alleleSplitter,
            cache
        )
        {
        }

        protected override IEnumerable<IHlaScoringMetadata> ConvertMetadataRowsToMetadata(
            IEnumerable<HlaMetadataTableRow> rows)
        {
            return rows.Select(row => row.ToHlaScoringMetadata());
        }

        protected override IHlaScoringMetadata ConsolidateHlaMetadata(
            Locus locus,
            string lookupName,
            IEnumerable<IHlaScoringMetadata> metadata)
        {
            var hlaTypingCategory = HlaCategorisationService.GetHlaTypingCategory(lookupName);
            var results = metadata.ToList();
            var scoringInfos = results.Select(result => result.HlaScoringInfo);

            switch (hlaTypingCategory)
            {
                case HlaTypingCategory.Allele:
                    return results.Count == 1
                        ? results.Single()
                        : GetMultipleAlleleMetadata(locus, lookupName, scoringInfos);

                case HlaTypingCategory.AlleleStringOfNames:
                case HlaTypingCategory.AlleleStringOfSubtypes:
                case HlaTypingCategory.NmdpCode:
                    return GetConsolidatedMolecularMetadata(locus, lookupName, scoringInfos);

                case HlaTypingCategory.XxCode:
                case HlaTypingCategory.Serology:
                    return results.Single();

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static IHlaScoringMetadata GetMultipleAlleleMetadata(
            Locus locus,
            string lookupName,
            IEnumerable<IHlaScoringInfo> scoringInfos)
        {
            var alleleScoringInfos = GetSingleAlleleScoringInfos(scoringInfos).ToList();
            var matchingSerologies = GetMatchingSerologies(alleleScoringInfos);

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
            IEnumerable<IHlaScoringInfo> scoringInfos)
        {
            var singleAlleleScoringInfos = GetSingleAlleleScoringInfos(scoringInfos).ToList();

            var matchingPGroups = GetMatchingPGroups(singleAlleleScoringInfos);
            var matchingGGroups = GetMatchingGGroups(singleAlleleScoringInfos);
            var matchingSerologies = GetMatchingSerologies(singleAlleleScoringInfos);

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

        private static IEnumerable<string> GetMatchingPGroups(
            IEnumerable<SingleAlleleScoringInfo> singleAlleleScoringInfos)
        {
            return singleAlleleScoringInfos
                .Select(single => single.MatchingPGroup)
                .Distinct();
        }

        private static IEnumerable<string> GetMatchingGGroups(
            IEnumerable<SingleAlleleScoringInfo> singleAlleleScoringInfos)
        {
            return singleAlleleScoringInfos
                .Select(single => single.MatchingGGroup)
                .Distinct();
        }

        private static IEnumerable<SerologyEntry> GetMatchingSerologies(
            IEnumerable<SingleAlleleScoringInfo> singleAlleleScoringInfos)
        {
            return singleAlleleScoringInfos
                .SelectMany(info => info.MatchingSerologies)
                .Distinct();
        }
    }
}