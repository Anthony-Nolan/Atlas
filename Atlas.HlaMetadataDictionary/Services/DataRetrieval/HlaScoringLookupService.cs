using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.HlaMetadataDictionary.Extensions;
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
    public interface IHlaScoringLookupService : IHlaSearchingLookupService<IHlaScoringLookupResult>
    {
    }

    public class HlaScoringLookupService :
        HlaSearchingLookupServiceBase<IHlaScoringLookupResult>,
        IHlaScoringLookupService
    {
        public HlaScoringLookupService(
            IHlaScoringLookupRepository hlaScoringLookupRepository,
            IAlleleNamesLookupService alleleNamesLookupService,
            IHlaCategorisationService hlaCategorisationService,
            IAlleleStringSplitterService alleleSplitter,
            INmdpCodeCache cache
        ) : base(
            hlaScoringLookupRepository,
            alleleNamesLookupService,
            hlaCategorisationService,
            alleleSplitter,
            cache
        )
        {
        }

        protected override IEnumerable<IHlaScoringLookupResult> ConvertTableEntitiesToLookupResults(
            IEnumerable<HlaLookupTableEntity> lookupTableEntities)
        {
            return lookupTableEntities.Select(entity =>
            {
                var scoringInfo = entity.DeserialiseTypedScoringInfo();

                return new HlaScoringLookupResult(
                    entity.Locus,
                    entity.LookupName,
                    scoringInfo,
                    entity.HlaInfoTypeSerialised);
            });
        }

        protected override IHlaScoringLookupResult ConsolidateHlaLookupResults(
            Locus locus,
            string lookupName,
            IEnumerable<IHlaScoringLookupResult> lookupResults)
        {
            var hlaTypingCategory = HlaCategorisationService.GetHlaTypingCategory(lookupName);
            var results = lookupResults.ToList();
            var scoringInfos = results.Select(result => result.HlaScoringInfo);

            switch (hlaTypingCategory)
            {
                case HlaTypingCategory.Allele:
                    return results.Count == 1
                        ? results.Single()
                        : GetMultipleAlleleLookupResult(locus, lookupName, scoringInfos);

                case HlaTypingCategory.AlleleStringOfNames:
                case HlaTypingCategory.AlleleStringOfSubtypes:
                case HlaTypingCategory.NmdpCode:
                    return GetConsolidatedMolecularLookupResult(locus, lookupName, scoringInfos);

                case HlaTypingCategory.XxCode:
                case HlaTypingCategory.Serology:
                    return results.Single();

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static IHlaScoringLookupResult GetMultipleAlleleLookupResult(
            Locus locus,
            string lookupName,
            IEnumerable<IHlaScoringInfo> scoringInfos)
        {
            var alleleScoringInfos = GetSingleAlleleScoringInfos(scoringInfos).ToList();
            var matchingSerologies = GetMatchingSerologies(alleleScoringInfos);

            var multipleAlleleScoringInfo = new MultipleAlleleScoringInfo(
                alleleScoringInfos,
                matchingSerologies);

            return new HlaScoringLookupResult(
                locus,
                lookupName,
                multipleAlleleScoringInfo
            );
        }

        private static IHlaScoringLookupResult GetConsolidatedMolecularLookupResult(
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

            return new HlaScoringLookupResult(
                locus,
                lookupName,
                consolidatedMolecularScoringInfo
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