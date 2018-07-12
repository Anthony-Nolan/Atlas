using Microsoft.Extensions.Caching.Memory;
using Nova.HLAService.Client;
using Nova.HLAService.Client.Services;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services.MatchingDictionary.Lookups;
using Nova.Utils.ApplicationInsights;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
{
    /// <summary>
    /// Determines and executes the dictionary lookup strategy for submitted HLA types.
    /// </summary>
    public interface IMatchingDictionaryLookupService
    {
        /// <summary>
        ///  Consolidates all hla used in matching for all alleles that map to the hla name
        /// </summary>
        Task<IMatchingHlaLookupResult> GetMatchingHla(MatchLocus matchLocus, string hlaName);

        /// <summary>
        ///  Expands the hla name into a list of matching dictionary entries
        /// </summary>
        /// <returns>A matching dictionary data for each hla typing that maps to the hla name</returns>
        Task<IEnumerable<MatchingDictionaryEntry>> GetMatchingDictionaryEntries(MatchLocus matchLocus, string hlaName);
    }

    public class MatchingDictionaryLookupService : LookupServiceBase<MatchingDictionaryEntry>, IMatchingDictionaryLookupService
    {
        private readonly IMatchingDictionaryRepository dictionaryRepository;
        private readonly IAlleleNamesLookupService alleleNamesLookupService;
        private readonly IHlaServiceClient hlaServiceClient;
        private readonly IHlaCategorisationService hlaCategorisationService;
        private readonly IAlleleStringSplitterService alleleSplitter;
        private readonly IMemoryCache memoryCache;
        private readonly ILogger logger;

        public MatchingDictionaryLookupService(
            IMatchingDictionaryRepository dictionaryRepository,
            IAlleleNamesLookupService alleleNamesLookupService,
            IHlaServiceClient hlaServiceClient,
            IHlaCategorisationService hlaCategorisationService,
            IAlleleStringSplitterService alleleSplitter,
            IMemoryCache memoryCache,
            ILogger logger
        )
        {
            this.dictionaryRepository = dictionaryRepository;
            this.alleleNamesLookupService = alleleNamesLookupService;
            this.hlaServiceClient = hlaServiceClient;
            this.hlaCategorisationService = hlaCategorisationService;
            this.alleleSplitter = alleleSplitter;
            this.memoryCache = memoryCache;
            this.logger = logger;
        }

        public async Task<IMatchingHlaLookupResult> GetMatchingHla(MatchLocus matchLocus, string hlaName)
        {
            // TODO: NOVA-1445: need to properly consolidate results from GetLookupResults
            var lookupResults = await GetLookupResults(matchLocus, hlaName);
            return lookupResults.FirstOrDefault();
        }

        public async Task<IEnumerable<MatchingDictionaryEntry>> GetMatchingDictionaryEntries(MatchLocus matchLocus, string hlaName)
        {
            return await GetLookupResults(matchLocus, hlaName);
        }

        protected override bool LookupNameIsValid(string lookupName)
        {
            return !string.IsNullOrEmpty(lookupName);
        }

        protected override async Task<IEnumerable<MatchingDictionaryEntry>> PerformLookup(MatchLocus matchLocus, string lookupName)
        {
            var dictionaryLookup = GetMatchingDictionaryLookup(lookupName);

            // TODO: NOVA-1445: lookup should return a list of non-consolidated entries
            var lookupResult = await dictionaryLookup.PerformLookupAsync(matchLocus, lookupName);

            return new List<MatchingDictionaryEntry> { lookupResult };
        }

        private MatchingDictionaryLookup GetMatchingDictionaryLookup(string lookupName)
        {
            var hlaTypingCategory = hlaCategorisationService.GetHlaTypingCategory(lookupName);

            return MatchingDictionaryLookupFactory
                .GetMatchingDictionaryLookupByHlaTypingCategory(
                    hlaTypingCategory,
                    dictionaryRepository,
                    alleleNamesLookupService,
                    hlaServiceClient,
                    hlaCategorisationService,
                    alleleSplitter,
                    memoryCache,
                    logger);
        }
    }
}