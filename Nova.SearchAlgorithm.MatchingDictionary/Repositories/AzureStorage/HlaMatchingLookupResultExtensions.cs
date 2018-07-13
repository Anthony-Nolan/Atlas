using System.Collections.Generic;
using Newtonsoft.Json;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.MatchingLookup;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    internal static class HlaMatchingLookupResultExtensions
    {
        internal static HlaLookupTableEntity ToTableEntity(this HlaMatchingLookupResult lookupResult)
        {
            return new HlaLookupTableEntity(lookupResult.MatchLocus, lookupResult.LookupName, lookupResult.TypingMethod)
            {
                SerialisedHlaInfo = JsonConvert.SerializeObject(lookupResult.MatchingPGroups)
            };
        }

        internal static HlaMatchingLookupResult ToHlaMatchingLookupResult(this HlaLookupTableEntity entity)
        {
            var matchingPGroups = JsonConvert.DeserializeObject<IEnumerable<string>>(entity.SerialisedHlaInfo);

            return new HlaMatchingLookupResult(entity.MatchLocus, entity.LookupName, entity.TypingMethod, matchingPGroups);
        }
    }
}