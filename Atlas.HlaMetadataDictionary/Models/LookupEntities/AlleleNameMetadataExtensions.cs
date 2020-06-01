using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.Models.LookupEntities;
using Atlas.HlaMetadataDictionary.Models.Lookups.AlleleNameLookup;

//QQ Path & Namespace - entities
namespace Atlas.HlaMetadataDictionary.Extensions
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