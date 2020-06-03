using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;

namespace Atlas.HlaMetadataDictionary.InternalModels.MetadataTableRows
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