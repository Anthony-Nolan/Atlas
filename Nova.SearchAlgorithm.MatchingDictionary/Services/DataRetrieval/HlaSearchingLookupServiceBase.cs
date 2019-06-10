using Microsoft.Extensions.Caching.Memory;
using Nova.HLAService.Client;
using Nova.HLAService.Client.Services;
using Nova.SearchAlgorithm.Common.Models;
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
        Task<THlaLookupResult> GetHlaLookupResult(Locus locus, string hlaName, string hlaDatabaseVersion);
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
        protected readonly IHlaCategorisationService HlaCategorisationService;

        private readonly IHlaLookupRepository hlaLookupRepository;
        private readonly IAlleleNamesLookupService alleleNamesLookupService;
        private readonly IHlaServiceClient hlaServiceClient;
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
            HlaCategorisationService = hlaCategorisationService;
            this.alleleSplitter = alleleSplitter;
            this.memoryCache = memoryCache;
            this.logger = logger;
        }

        public async Task<THlaLookupResult> GetHlaLookupResult(Locus locus, string hlaName, string hlaDatabaseVersion)
        {
            return await GetLookupResults(locus, hlaName, hlaDatabaseVersion);
        }

        protected override bool LookupNameIsValid(string lookupName)
        {
            return !string.IsNullOrEmpty(lookupName);
        }

        protected override async Task<THlaLookupResult> PerformLookup(Locus locus, string lookupName, string hlaDatabaseVersion)
        {
            return await GetSingleHlaLookupResult(locus, lookupName, hlaDatabaseVersion);
        }

        private async Task<THlaLookupResult> GetSingleHlaLookupResult(Locus locus, string lookupName, string hlaDatabaseVersion)
        {
            var dictionaryLookup = GetHlaLookup(lookupName);
            var lookupTableEntities = await dictionaryLookup.PerformLookupAsync(locus, lookupName, hlaDatabaseVersion);
            var lookupResults = ConvertTableEntitiesToLookupResults(lookupTableEntities);

            return ConsolidateHlaLookupResults(locus, lookupName, lookupResults);
        }

        private HlaLookupBase GetHlaLookup(string lookupName)
        {
            var hlaTypingCategory = HlaCategorisationService.GetHlaTypingCategory(lookupName);

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
            Locus locus,
            string lookupName,
            IEnumerable<THlaLookupResult> lookupResults);
    }
}