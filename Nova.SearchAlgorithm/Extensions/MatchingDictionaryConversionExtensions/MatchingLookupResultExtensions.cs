using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.MatchingLookup;

namespace Nova.SearchAlgorithm.Extensions.MatchingDictionaryConversionExtensions
{
    public static class MatchingLookupResultExtensions
    {
        public static ExpandedHla ToExpandedHla(this IHlaMatchingLookupResult lookupResult, string originalName)
        {
            return new ExpandedHla
            {
                LookupName = lookupResult.LookupName,
                OriginalName = originalName,
                Locus = lookupResult.MatchLocus.ToLocus(),
                PGroups = lookupResult.MatchingPGroups
            };
        }
    }
}