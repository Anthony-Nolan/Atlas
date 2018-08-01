using Microsoft.Extensions.Caching.Memory;
using Nova.HLAService.Client;
using Nova.HLAService.Client.Models;
using Nova.HLAService.Client.Services;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using Nova.Utils.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
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
            IHlaServiceClient hlaServiceClient,
            IHlaCategorisationService hlaCategorisationService,
            IAlleleStringSplitterService alleleSplitter,
            IMemoryCache memoryCache,
            ILogger logger
        ) : base(
            hlaScoringLookupRepository,
            alleleNamesLookupService,
            hlaServiceClient,
            hlaCategorisationService,
            alleleSplitter,
            memoryCache,
            logger
        )
        {
        }

        protected override IEnumerable<IHlaScoringLookupResult> ConvertTableEntitiesToLookupResults(
            IEnumerable<HlaLookupTableEntity> lookupTableEntities)
        {
            return lookupTableEntities.Select(entity => entity.ToHlaScoringLookupResult());
        }

        protected override IHlaScoringLookupResult ConsolidateHlaLookupResults(
            MatchLocus matchLocus,
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
                        : GetMultipleAlleleLookupResult(matchLocus, lookupName, scoringInfos);

                case HlaTypingCategory.AlleleStringOfNames:
                case HlaTypingCategory.AlleleStringOfSubtypes:
                case HlaTypingCategory.NmdpCode:
                    return GetConsolidatedMolecularLookupResult(matchLocus, lookupName, scoringInfos);

                case HlaTypingCategory.XxCode:
                case HlaTypingCategory.Serology:
                    return results.Single();

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static IHlaScoringLookupResult GetMultipleAlleleLookupResult(
            MatchLocus matchLocus,
            string lookupName,
            IEnumerable<IHlaScoringInfo> scoringInfos)
        {
            var alleleScoringInfos = GetSingleAlleleScoringInfos(scoringInfos).ToList();
            var matchingSerologies = GetMatchingSerologies(alleleScoringInfos);

            var multipleAlleleScoringInfo = new MultipleAlleleScoringInfo(
                alleleScoringInfos,
                matchingSerologies);

            return new HlaScoringLookupResult(
                matchLocus,
                lookupName,
                LookupNameCategory.MultipleAlleles,
                multipleAlleleScoringInfo
            );
        }

        private static IHlaScoringLookupResult GetConsolidatedMolecularLookupResult(
            MatchLocus matchLocus,
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
                matchLocus,
                lookupName,
                LookupNameCategory.MultipleAlleles,
                consolidatedMolecularScoringInfo
            );
        }

        private static IEnumerable<SingleAlleleScoringInfo> GetSingleAlleleScoringInfos(
            IEnumerable<IHlaScoringInfo> scoringInfos)
        {
            var infos = scoringInfos.ToList();

            return infos.OfType<SingleAlleleScoringInfo>()
                .Union(infos.OfType<MultipleAlleleScoringInfo>()
                    .SelectMany(multiple => multiple.AlleleScoringInfos));
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