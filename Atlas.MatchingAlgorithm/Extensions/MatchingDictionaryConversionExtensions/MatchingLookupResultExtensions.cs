using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.HlaMetadataDictionary.Models.Lookups.MatchingLookup;

namespace Atlas.MatchingAlgorithm.Extensions.MatchingDictionaryConversionExtensions
{
    public static class MatchingLookupResultExtensions
    {
        public static ExpandedHla ToExpandedHla(this IHlaMatchingLookupResult lookupResult, string originalName)
        {
            return new ExpandedHla
            {
                LookupName = lookupResult.LookupName,
                OriginalName = originalName,
                Locus = lookupResult.Locus,
                PGroups = lookupResult.MatchingPGroups
            };
        }
    }
}