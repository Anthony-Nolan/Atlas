using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.Models.LookupEntities;
using Atlas.HlaMetadataDictionary.Models.Lookups.MatchingLookup;

namespace Atlas.HlaMetadataDictionary.Extensions
{
    internal static class HlaMatchingLookupResultExtensions
    {
        public static IHlaMatchingLookupResult ToHlaMatchingLookupResult(this HlaLookupTableEntity entity)
        {
            var matchingPGroups = entity.GetHlaInfo<IEnumerable<string>>();

            return new HlaMatchingLookupResult(
                entity.Locus, 
                entity.LookupName, 
                entity.TypingMethod, 
                matchingPGroups);
        }
    }
}