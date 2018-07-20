using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.MatchingLookup;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    internal static class HlaMatchingLookupResultExtensions
    {
        internal static HlaLookupTableEntity ToTableEntity(this HlaMatchingLookupResult lookupResult)
        {
            return new HlaLookupTableEntity(lookupResult, lookupResult.MatchingPGroups);
        }

        internal static HlaMatchingLookupResult ToHlaMatchingLookupResult(this HlaLookupTableEntity entity)
        {
            var matchingPGroups = entity.GetHlaInfo<IEnumerable<string>>();

            return new HlaMatchingLookupResult(
                entity.MatchLocus, 
                entity.LookupName, 
                entity.TypingMethod, 
                matchingPGroups);
        }
    }
}