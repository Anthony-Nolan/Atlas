using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.Models.LookupEntities;
using Atlas.HlaMetadataDictionary.Models.Lookups.MatchingLookup;

namespace Atlas.HlaMetadataDictionary.Extensions
{
    internal static class HlaMatchingMetadataExtensions
    {
        public static IHlaMatchingMetadata ToHlaMatchingMetadata(this HlaMetadataTableRow row)
        {
            var matchingPGroups = row.GetHlaInfo<List<string>>();

            return new HlaMatchingMetadata(
                row.Locus, 
                row.LookupName, 
                row.TypingMethod, 
                matchingPGroups);
        }
    }
}