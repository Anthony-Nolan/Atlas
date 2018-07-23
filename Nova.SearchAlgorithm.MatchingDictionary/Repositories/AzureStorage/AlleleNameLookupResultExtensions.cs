using Newtonsoft.Json;
using System.Collections.Generic;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.AlleleNameLookup;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    internal static class AlleleNameLookupResultExtensions
    {
        internal static HlaLookupTableEntity ToTableEntity(this AlleleNameLookupResult lookupResult)
        {
            return new HlaLookupTableEntity(lookupResult.MatchLocus, lookupResult.LookupName, lookupResult.TypingMethod)
            {
                SerialisedHlaInfo = JsonConvert.SerializeObject(lookupResult.CurrentAlleleNames)
            };
        }

        internal static AlleleNameLookupResult ToAlleleNameLookupResult(this HlaLookupTableEntity result)
        {
            var currentAlleleNames = JsonConvert.DeserializeObject<IEnumerable<string>>(result.SerialisedHlaInfo);

            return new AlleleNameLookupResult(result.MatchLocus, result.LookupName, currentAlleleNames);
        }
    }
}