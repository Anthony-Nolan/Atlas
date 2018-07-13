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
                SerialisedHlaInfo = JsonConvert.SerializeObject(lookupResult)
            };
        }

        internal static HlaMatchingLookupResult ToHlaMatchingLookupResult(this HlaLookupTableEntity result)
        {
            return JsonConvert.DeserializeObject<HlaMatchingLookupResult>(result.SerialisedHlaInfo);
        }
    }
}