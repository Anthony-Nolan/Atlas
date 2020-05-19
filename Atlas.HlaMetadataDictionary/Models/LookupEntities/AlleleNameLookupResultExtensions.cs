using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.Models.LookupEntities;
using Atlas.HlaMetadataDictionary.Models.Lookups.AlleleNameLookup;

namespace Atlas.HlaMetadataDictionary.Extensions
{
    internal static class AlleleNameLookupResultExtensions
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