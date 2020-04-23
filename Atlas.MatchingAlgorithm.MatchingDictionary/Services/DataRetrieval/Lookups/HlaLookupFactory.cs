using Atlas.MatchingAlgorithm.Common.Services;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.MatchingDictionary.Caching;
using Atlas.MatchingAlgorithm.MatchingDictionary.Repositories;
using System;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Services.Lookups
{
    internal static class HlaLookupFactory
    {
        public static HlaLookupBase GetLookupByHlaTypingCategory(
            HlaTypingCategory category,
            IHlaLookupRepository hlaLookupRepository,
            IAlleleNamesLookupService alleleNamesLookupService,
            IAlleleStringSplitterService alleleSplitter,
            INmdpCodeCache cache)
        {
            switch (category)
            {
                case HlaTypingCategory.Allele:
                    return new SingleAlleleLookup(
                        hlaLookupRepository, 
                        alleleNamesLookupService);
                case HlaTypingCategory.XxCode:
                    return new XxCodeLookup(
                        hlaLookupRepository);
                case HlaTypingCategory.Serology:
                    return new SerologyLookup(
                        hlaLookupRepository);                   
                case HlaTypingCategory.NmdpCode:
                    return new NmdpCodeLookup(
                        hlaLookupRepository,
                        alleleNamesLookupService,
                        cache);                    
                case HlaTypingCategory.AlleleStringOfNames:
                case HlaTypingCategory.AlleleStringOfSubtypes:
                    return new AlleleStringLookup(
                        hlaLookupRepository, 
                        alleleNamesLookupService, 
                        alleleSplitter);                    
                default:
                    throw new ArgumentException(
                        $"Dictionary lookup cannot be performed for HLA typing category: {category}.");
            }
        }
    }
}