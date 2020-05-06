using Atlas.MatchingAlgorithm.MatchingDictionary.Models.Lookups.AlleleNameLookup;
using System.Collections.Generic;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    public static class AlleleNameLookupResultExtensions
    {
        public static IAlleleNameLookupResult ToAlleleNameLookupResult(this HlaLookupTableEntity entity)
        {
            var currentAlleleNames = entity.GetHlaInfo<IEnumerable<string>>();

            return new AlleleNameLookupResult(
                entity.Locus, 
                entity.LookupName, 
                currentAlleleNames);
        }
    }
}