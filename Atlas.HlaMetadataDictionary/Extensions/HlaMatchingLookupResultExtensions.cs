using Atlas.HlaMetadataDictionary.Models.Lookups.MatchingLookup;
using System.Collections.Generic;

namespace Atlas.HlaMetadataDictionary.Repositories.AzureStorage
{
    public static class HlaMatchingLookupResultExtensions
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