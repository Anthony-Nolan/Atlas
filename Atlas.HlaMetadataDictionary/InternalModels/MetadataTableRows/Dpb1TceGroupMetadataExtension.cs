using Atlas.HlaMetadataDictionary.InternalModels.Metadata;

namespace Atlas.HlaMetadataDictionary.InternalModels.MetadataTableRows
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