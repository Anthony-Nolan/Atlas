using Microsoft.Extensions.Caching.Memory;
using Nova.HLAService.Client;
using Nova.HLAService.Client.Services;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.MatchingLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services.Lookups;
using Nova.Utils.ApplicationInsights;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
{
    public interface IHlaMatchingLookupService
    {
        /// <summary>
        ///  Consolidates all hla used in matching for all alleles that map to the hla name
        /// </summary>
        Task<IHlaMatchingLookupResult> GetHlaMatchingLookupResult(MatchLocus matchLocus, string hlaName);
    }

    public class HlaMatchingLookupService : 
        LookupServiceBase<IHlaMatchingLookupResult>, IHlaMatchingLookupService
    {
        private readonly IHlaMatchingLookupRepository hlaMatchingLookupRepository;
        private readonly IAlleleNamesLookupService alleleNamesLookupService;
        private readonly IHlaServiceClient hlaServiceClient;
        private readonly IHlaCategorisationService hlaCategorisationService;
        private readonly IAlleleStringSplitterService alleleSplitter;
        private readonly IMemoryCache memoryCache;
        private readonly ILogger logger;

        public HlaMatchingLookupService(
            IHlaMatchingLookupRepository hlaMatchingLookupRepository,
            IAlleleNamesLookupService alleleNamesLookupService,
            IHlaServiceClient hlaServiceClient,
            IHlaCategorisationService hlaCategorisationService,
            IAlleleStringSplitterService alleleSplitter,
            IMemoryCache memoryCache,
            ILogger logger
        )
        {
            this.hlaMatchingLookupRepository = hlaMatchingLookupRepository;
            this.alleleNamesLookupService = alleleNamesLookupService;
            this.hlaServiceClient = hlaServiceClient;
            this.hlaCategorisationService = hlaCategorisationService;
            this.alleleSplitter = alleleSplitter;
            this.memoryCache = memoryCache;
            this.logger = logger;
        }

        public async Task<IHlaMatchingLookupResult> GetHlaMatchingLookupResult(MatchLocus matchLocus, string hlaName)
        {
            var lookupResults = await GetLookupResults(matchLocus, hlaName);
            return lookupResults.FirstOrDefault();
        }

        protected override bool LookupNameIsValid(string lookupName)
        {
            return !string.IsNullOrEmpty(lookupName);
        }

        protected override async Task<IEnumerable<IHlaMatchingLookupResult>> PerformLookup(MatchLocus matchLocus, string lookupName)
        {
            var dictionaryLookup = GetHlaMatchingLookup(lookupName);
            var lookupResult = await dictionaryLookup.PerformLookupAsync(matchLocus, lookupName);

            return new List<IHlaMatchingLookupResult> { lookupResult };
        }

        private HlaMatchingLookupBase GetHlaMatchingLookup(string lookupName)
        {
            var hlaTypingCategory = hlaCategorisationService.GetHlaTypingCategory(lookupName);

            return HlaMatchingLookupFactory
                .GetLookupByHlaTypingCategory(
                    hlaTypingCategory,
                    hlaMatchingLookupRepository,
                    alleleNamesLookupService,
                    hlaServiceClient,
                    hlaCategorisationService,
                    alleleSplitter,
                    memoryCache,
                    logger);
        }
    }
}