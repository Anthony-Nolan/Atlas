using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.MatchingLookup;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    public static class HlaMatchingLookupResultExtensions
    {
        public static IHlaMatchingLookupResult ToHlaMatchingLookupResult(this HlaLookupTableEntity entity)
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