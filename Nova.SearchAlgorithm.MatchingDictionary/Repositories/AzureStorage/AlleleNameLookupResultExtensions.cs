using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.AlleleNameLookup;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    public static class AlleleNameLookupResultExtensions
    {
        public static HlaLookupTableEntity ToTableEntity(this IAlleleNameLookupResult lookupResult)
        {
            return new HlaLookupTableEntity(lookupResult);
        }

        public static IAlleleNameLookupResult ToAlleleNameLookupResult(this HlaLookupTableEntity entity)
        {
            var currentAlleleNames = entity.GetHlaInfo<IEnumerable<string>>();

            return new AlleleNameLookupResult(
                entity.MatchLocus, 
                entity.LookupName, 
                currentAlleleNames);
        }
    }
}