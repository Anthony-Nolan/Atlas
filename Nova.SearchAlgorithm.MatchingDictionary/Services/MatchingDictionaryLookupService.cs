using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Nova.HLAService.Client;
using Nova.HLAService.Client.Models;
using Nova.HLAService.Client.Services;
using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services.MatchingDictionary.Lookups;
using Nova.Utils.ApplicationInsights;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
{
    /// <summary>
    /// Determines and executes the dictionary lookup strategy for submitted HLA types.
    /// </summary>
    public interface IMatchingDictionaryLookupService
    {
        Task<IMatchingHlaLookupResult> GetMatchingHla(MatchLocus matchLocus, string hlaName);
    }

    public class MatchingDictionaryLookupService : IMatchingDictionaryLookupService
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
            if (string.IsNullOrEmpty(hlaName))
            {
                throw new MatchingDictionaryException($"Cannot lookup null or blank HLA (locus was {matchLocus})");
            }

            try
            {
                var lookupName = hlaName.Trim().TrimStart('*');
                var category = hlaCategorisationService.GetHlaTypingCategory(lookupName);

                MatchingDictionaryLookup lookup;
                switch (category)
                {
                    case HlaTypingCategory.Allele:
                        lookup = new AlleleLookup(dictionaryRepository, alleleNamesLookupService);
                        break;
                    case HlaTypingCategory.XxCode:
                        lookup = new XxCodeLookup(dictionaryRepository);
                        break;
                    case HlaTypingCategory.Serology:
                        lookup = new SerologyLookup(dictionaryRepository);
                        break;
                    case HlaTypingCategory.NmdpCode:
                        lookup = new NmdpCodeLookup(
                            dictionaryRepository,
                            alleleNamesLookupService,
                            memoryCache,
                            hlaServiceClient,
                            alleleSplitter,
                            logger);
                        break;
                    case HlaTypingCategory.AlleleStringOfNames:
                    case HlaTypingCategory.AlleleStringOfSubtypes:
                        lookup = new AlleleStringLookup(dictionaryRepository, alleleNamesLookupService, alleleSplitter);
                        break;
                    default:
                        throw new ArgumentException(
                            $"Dictionary lookup cannot be performed for HLA typing category: {category}.");
                }

                return await lookup.PerformLookupAsync(matchLocus, lookupName);
            }
            catch (Exception ex)
            {
                var msg = $"Failed to get matching HLA for {hlaName} at locus {matchLocus}.";
                throw new MatchingDictionaryException(msg, ex);
            }
        }
    }
}