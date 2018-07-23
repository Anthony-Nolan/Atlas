using Microsoft.Extensions.Caching.Memory;
using Nova.HLAService.Client;
using Nova.HLAService.Client.Services;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using Nova.SearchAlgorithm.MatchingDictionary.Services.Lookups;
using Nova.Utils.ApplicationInsights;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
{
    public interface IHlaSearchingLookupService<THlaLookupResult>
        where THlaLookupResult : IHlaLookupResult
    {
        Task<THlaLookupResult> GetHlaLookupResult(MatchLocus matchLocus, string hlaName);
    }

    /// <summary>
    /// Common functionality used when querying a HLA 'searching' 
    /// (i.e., matching or scoring) lookup repository.
    /// </summary>
    public abstract class HlaSearchingLookupServiceBase<THlaLookupResult> :
        LookupServiceBase<THlaLookupResult>,
        IHlaSearchingLookupService<THlaLookupResult>
        where THlaLookupResult : IHlaLookupResult
    {
        private readonly IHlaLookupRepository hlaLookupRepository;
        private readonly IAlleleNamesLookupService alleleNamesLookupService;
        private readonly IHlaServiceClient hlaServiceClient;
        private readonly IHlaCategorisationService hlaCategorisationService;
        private readonly IAlleleStringSplitterService alleleSplitter;
        private readonly IMemoryCache memoryCache;
        private readonly ILogger logger;

        protected HlaSearchingLookupServiceBase(
            IHlaLookupRepository hlaLookupRepository,
            IAlleleNamesLookupService alleleNamesLookupService,
            IHlaServiceClient hlaServiceClient,
            IHlaCategorisationService hlaCategorisationService,
            IAlleleStringSplitterService alleleSplitter,
            IMemoryCache memoryCache,
            ILogger logger
        )
        {
            this.hlaLookupRepository = hlaLookupRepository;
            this.alleleNamesLookupService = alleleNamesLookupService;
            this.hlaServiceClient = hlaServiceClient;
            this.hlaCategorisationService = hlaCategorisationService;
            this.alleleSplitter = alleleSplitter;
            this.memoryCache = memoryCache;
            this.logger = logger;
        }

        public async Task<THlaLookupResult> GetHlaLookupResult(MatchLocus matchLocus, string hlaName)
        {
            return await GetLookupResults(matchLocus, hlaName);
        }

        protected override bool LookupNameIsValid(string lookupName)
        {
            return !string.IsNullOrEmpty(lookupName);
        }

        protected override async Task<THlaLookupResult> PerformLookup(MatchLocus matchLocus, string lookupName)
        {
            return await GetSingleHlaLookupResult(matchLocus, lookupName);
        }

        private async Task<THlaLookupResult> GetSingleHlaLookupResult(MatchLocus matchLocus, string lookupName)
        {
            var dictionaryLookup = GetHlaLookup(lookupName);
            var lookupTableEntities = await dictionaryLookup.PerformLookupAsync(matchLocus, lookupName);
            var lookupResults = ConvertTableEntitiesToLookupResults(lookupTableEntities);

            return ConsolidateHlaLookupResults(matchLocus, lookupName, lookupResults);
        }

        private HlaLookupBase GetHlaLookup(string lookupName)
        {
            var hlaTypingCategory = hlaCategorisationService.GetHlaTypingCategory(lookupName);

            return HlaLookupFactory
                .GetLookupByHlaTypingCategory(
                    hlaTypingCategory,
                    hlaLookupRepository,
                    alleleNamesLookupService,
                    hlaServiceClient,
                    alleleSplitter,
                    memoryCache,
                    logger);
        }

        protected abstract IEnumerable<THlaLookupResult> ConvertTableEntitiesToLookupResults(
            IEnumerable<HlaLookupTableEntity> hlaLookupTableEntities);

        protected abstract THlaLookupResult ConsolidateHlaLookupResults(
            MatchLocus matchLocus,
            string lookupName,
            IEnumerable<THlaLookupResult> lookupResults);
    }
}