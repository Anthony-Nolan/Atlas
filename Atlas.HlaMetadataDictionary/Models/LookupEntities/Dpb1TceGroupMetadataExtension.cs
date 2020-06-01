using Atlas.HlaMetadataDictionary.Models.LookupEntities;
using Atlas.HlaMetadataDictionary.Models.Lookups.Dpb1TceGroupLookup;

namespace Atlas.HlaMetadataDictionary.Extensions
{
    internal static class Dpb1TceGroupMetadataExtensions
    {
        public static IDpb1TceGroupsMetadata ToDpb1TceGroupMetadata(this HlaMetadataTableRow row)
        {
            var tceGroup = row.GetHlaInfo<string>();

            return new Dpb1TceGroupsMetadata(
                row.LookupName, 
                tceGroup);
        }
    }
}