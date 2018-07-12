using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;

namespace Nova.SearchAlgorithm.MatchingDictionaryConversions
{
    public static class MatchingLookupResultExtensions
    {
        public static ExpandedHla ToExpandedHla(this IMatchingHlaLookupResult lookupResult, string originalName)
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