using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;

namespace Atlas.HlaMetadataDictionary.InternalModels.MetadataTableRows
{
    internal static class SmallGGroupMetadataExtensions
    {
        public static ISmallGGroupsMetadata ToSmallGGroupMetadata(this HlaMetadataTableRow row)
        {
            var smallGGroups = row.GetHlaInfo<List<string>>();

            return new SmallGGroupsMetadata(
                row.Locus,
                row.LookupName,
                row.TypingMethod,
                smallGGroups);
        }
    }
}