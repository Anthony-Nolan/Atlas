using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.AlleleNameLookup;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    internal static class AlleleNameLookupResultExtensions
    {
        internal static HlaLookupTableEntity ToTableEntity(this AlleleNameLookupResult lookupResult)
        {
            return new HlaLookupTableEntity(lookupResult, lookupResult.CurrentAlleleNames);
        }

        internal static AlleleNameLookupResult ToAlleleNameLookupResult(this HlaLookupTableEntity entity)
        {
            var currentAlleleNames = entity.GetHlaInfo<IEnumerable<string>>();

            return new AlleleNameLookupResult(
                entity.MatchLocus, 
                entity.LookupName, 
                currentAlleleNames);
        }
    }
}