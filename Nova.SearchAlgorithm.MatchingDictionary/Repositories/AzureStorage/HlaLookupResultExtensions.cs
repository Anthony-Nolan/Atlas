using Newtonsoft.Json;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    internal static class HlaLookupResultExtensions
    {
        internal static HlaLookupTableEntity ToTableEntity(this IHlaLookupResult lookupResult)
        {
            return new HlaLookupTableEntity(lookupResult.MatchLocus, lookupResult.LookupName, lookupResult.TypingMethod)
            {
                SerialisedHlaLookupResult = JsonConvert.SerializeObject(lookupResult)
            };
        }

        internal static THlaLookupResult ToResult<THlaLookupResult>(this HlaLookupTableEntity result)
            where THlaLookupResult : IHlaLookupResult
        {
            return JsonConvert.DeserializeObject<THlaLookupResult>(result.SerialisedHlaLookupResult);
        }
    }
}