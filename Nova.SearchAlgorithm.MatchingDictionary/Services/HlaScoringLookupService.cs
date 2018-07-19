using Microsoft.Extensions.Caching.Memory;
using Nova.HLAService.Client;
using Nova.HLAService.Client.Models;
using Nova.HLAService.Client.Services;
using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using Nova.SearchAlgorithm.MatchingDictionary.Services.Lookups;
using Nova.Utils.ApplicationInsights;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
{
    /// <summary>
    /// Determines and executes the dictionary lookup strategy for submitted HLA types.
    /// </summary>
    public interface IHlaScoringLookupService
    {
        /// <summary>
        ///  Expands the hla name into a list of HLA scoring lookup results.
        /// </summary>
        /// <returns>A HLA Scoring Lookup Result for each HLA typing that maps to the HLA name.</returns>
        Task<IHlaScoringLookupResult> GetHlaScoringLookupResults(MatchLocus matchLocus, string hlaName);
    }

    public class HlaScoringLookupService :
        LookupServiceBase<IHlaScoringLookupResult>, IHlaScoringLookupService
    {
        private readonly IHlaScoringLookupRepository hlaScoringLookupRepository;
        private readonly IAlleleNamesLookupService alleleNamesLookupService;
        private readonly IHlaServiceClient hlaServiceClient;
        private readonly IHlaCategorisationService hlaCategorisationService;
        private readonly IAlleleStringSplitterService alleleSplitter;
        private readonly IMemoryCache memoryCache;
        private readonly ILogger logger;

        public HlaScoringLookupService(
            IHlaScoringLookupRepository hlaScoringLookupRepository,
            IAlleleNamesLookupService alleleNamesLookupService,
            IHlaServiceClient hlaServiceClient,
            IHlaCategorisationService hlaCategorisationService,
            IAlleleStringSplitterService alleleSplitter,
            IMemoryCache memoryCache,
            ILogger logger
        )
        {
            this.hlaScoringLookupRepository = hlaScoringLookupRepository;
            this.alleleNamesLookupService = alleleNamesLookupService;
            this.hlaServiceClient = hlaServiceClient;
            this.hlaCategorisationService = hlaCategorisationService;
            this.alleleSplitter = alleleSplitter;
            this.memoryCache = memoryCache;
            this.logger = logger;
        }

        public async Task<IHlaScoringLookupResult> GetHlaScoringLookupResults(MatchLocus matchLocus, string hlaName)
        {
            return await GetLookupResults(matchLocus, hlaName);
        }

        protected override bool LookupNameIsValid(string lookupName)
        {
            return !string.IsNullOrEmpty(lookupName);
        }

        protected override async Task<IHlaScoringLookupResult> PerformLookup(MatchLocus matchLocus, string lookupName)
        {
            var hlaTypingCategory = hlaCategorisationService.GetHlaTypingCategory(lookupName);
            var dictionaryLookup = GetHlaLookup(hlaTypingCategory);
            var lookupTableEntities = await dictionaryLookup.PerformLookupAsync(matchLocus, lookupName);
            var lookupResults = GetHlaLookupResults(matchLocus, lookupName, lookupTableEntities);

            return GetConsolidatedHlaLookupResult(matchLocus, lookupName, lookupResults, hlaTypingCategory);
        }

        private HlaLookupBase GetHlaLookup(HlaTypingCategory hlaTypingCategory)
        {
            return HlaLookupFactory
                .GetLookupByHlaTypingCategory(
                    hlaTypingCategory,
                    hlaScoringLookupRepository,
                    alleleNamesLookupService,
                    hlaServiceClient,
                    hlaCategorisationService,
                    alleleSplitter,
                    memoryCache,
                    logger);
        }

        private static IEnumerable<IHlaScoringLookupResult> GetHlaLookupResults(
            MatchLocus matchLocus,
            string lookupName,
            IEnumerable<HlaLookupTableEntity> lookupTableEntities)
        {
            var entities = lookupTableEntities.ToList();

            if (!entities.Any())
            {
                throw new InvalidHlaException(matchLocus, lookupName);
            }

            return entities.Select(entity => entity.ToHlaScoringLookupResult());
        }

        private static IHlaScoringLookupResult GetConsolidatedHlaLookupResult(
            MatchLocus matchLocus,
            string lookupName,
            IEnumerable<IHlaScoringLookupResult> lookupResults,
            HlaTypingCategory hlaTypingCategory)
        {
            var results = lookupResults.ToList();

            // only molecular typings have the potential to bring back >1 result
            var lookupResult = results.Count == 1
                ? results.First()
                : GetConsolidatedMolecularHlaLookupResult(matchLocus, lookupName, results);

            lookupResult.HlaTypingCategory = hlaTypingCategory;

            return lookupResult;
        }

        private static IHlaScoringLookupResult GetConsolidatedMolecularHlaLookupResult(
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

            var mergedMultipleAlleleScoringInfo = new MultipleAlleleScoringInfo(singleAlleleScoringInfos);

            return new HlaScoringLookupResult(
                matchLocus,
                lookupName,
                TypingMethod.Molecular,
                LookupResultCategory.MultipleAlleles,
                mergedMultipleAlleleScoringInfo
            );
        }
    }
}