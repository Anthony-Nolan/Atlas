using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;

//QQ Path & Namespace - entities
namespace Atlas.HlaMetadataDictionary.InternalModels.MetadataTableRows
{
    internal static class AlleleNameMetadataExtensions
    {
        public static IAlleleNameMetadata ToAlleleNameMetadata(this HlaMetadataTableRow row)
        {
            var currentAlleleNames = row.GetHlaInfo<List<string>>();

            return new AlleleNameMetadata(
                row.Locus, 
                row.LookupName, 
                currentAlleleNames);
        }
    }
}