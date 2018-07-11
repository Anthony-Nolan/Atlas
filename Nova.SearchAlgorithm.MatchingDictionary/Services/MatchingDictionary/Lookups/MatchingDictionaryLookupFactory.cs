using Microsoft.Extensions.Caching.Memory;
using Nova.HLAService.Client;
using Nova.HLAService.Client.Models;
using Nova.HLAService.Client.Services;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.Utils.ApplicationInsights;
using System;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.MatchingDictionary.Lookups
{
    internal static class MatchingDictionaryLookupFactory
    {
        public static MatchingDictionaryLookup GetMatchingDictionaryLookupByHlaTypingCategory(
            HlaTypingCategory category,
            IMatchingDictionaryRepository dictionaryRepository,
            IAlleleNamesLookupService alleleNamesLookupService,
            IHlaServiceClient hlaServiceClient,
            IHlaCategorisationService hlaCategorisationService,
            IAlleleStringSplitterService alleleSplitter,
            IMemoryCache memoryCache,
            ILogger logger)
        {
            MatchingDictionaryLookup lookup;
            switch (category)
            {
                case HlaTypingCategory.Allele:
                    lookup = new SingleAlleleLookup(dictionaryRepository, alleleNamesLookupService);
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

            return lookup;
        }
    }
}