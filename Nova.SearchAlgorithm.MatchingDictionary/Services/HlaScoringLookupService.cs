using Microsoft.Extensions.Caching.Memory;
using Nova.HLAService.Client;
using Nova.HLAService.Client.Services;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using Nova.Utils.ApplicationInsights;
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
            var results = lookupResults.ToList();

            // only molecular typings have the potential to bring back >1 result
            var lookupResult = results.Count == 1
                ? results.First()
                : GetMultipleAlleleLookupResult(matchLocus, lookupName, results);

            return lookupResult;
        }

        private static IHlaScoringLookupResult GetMultipleAlleleLookupResult(
            MatchLocus matchLocus,
            string lookupName,
            IEnumerable<IHlaScoringLookupResult> lookupResults)
        {
            var allScoringInfos = lookupResults
                .Select(result => result.HlaScoringInfo)
                .ToList();

            var singleAlleleScoringInfos = allScoringInfos.OfType<SingleAlleleScoringInfo>()
                    .Concat(allScoringInfos.OfType<MultipleAlleleScoringInfo>()
                        .SelectMany(info => info.AlleleScoringInfos))
                        .ToList();

            var multipleAlleleScoringInfo = new MultipleAlleleScoringInfo(singleAlleleScoringInfos);

            return new HlaScoringLookupResult(
                matchLocus,
                lookupName,
                LookupResultCategory.MultipleAlleles,
                multipleAlleleScoringInfo
            );
        }
    }
}