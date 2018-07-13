using Microsoft.Extensions.Caching.Memory;
using Nova.HLAService.Client;
using Nova.HLAService.Client.Models;
using Nova.HLAService.Client.Services;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.Utils.ApplicationInsights;
using System;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.MatchingDictionary.Lookups
{
    internal static class HlaMatchingLookupFactory
    {
        public static HlaMatchingLookupBase GetLookupByHlaTypingCategory(
            HlaTypingCategory category,
            IHlaMatchingLookupRepository hlaMatchingLookupRepository,
            IAlleleNamesLookupService alleleNamesLookupService,
            IHlaServiceClient hlaServiceClient,
            IHlaCategorisationService hlaCategorisationService,
            IAlleleStringSplitterService alleleSplitter,
            IMemoryCache memoryCache,
            ILogger logger)
        {
            switch (category)
            {
                case HlaTypingCategory.Allele:
                    return new SingleAlleleLookup(hlaMatchingLookupRepository, alleleNamesLookupService);
                case HlaTypingCategory.XxCode:
                    return new XxCodeLookup(hlaMatchingLookupRepository);
                case HlaTypingCategory.Serology:
                    return new SerologyLookup(hlaMatchingLookupRepository);                   
                case HlaTypingCategory.NmdpCode:
                    return new NmdpCodeLookup(
                        hlaMatchingLookupRepository,
                        alleleNamesLookupService,
                        memoryCache,
                        hlaServiceClient,
                        alleleSplitter,
                        logger);                    
                case HlaTypingCategory.AlleleStringOfNames:
                case HlaTypingCategory.AlleleStringOfSubtypes:
                    return new AlleleStringLookup(hlaMatchingLookupRepository, alleleNamesLookupService, alleleSplitter);
                    
                default:
                    throw new ArgumentException(
                        $"Dictionary lookup cannot be performed for HLA typing category: {category}.");
            }
        }
    }
}