using System.Collections.Generic;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;

namespace Atlas.HlaMetadataDictionary.InternalModels.MetadataTableRows
{
    internal static class SerologyToAllelesMetadataExtensions
    {
        public static ISerologyToAllelesMetadata ToSerologyToAlleleMetadata(this HlaMetadataTableRow row)
        {
            var mappings = row.GetHlaInfo<List<SerologyToAlleleMappingSummary>>();

            return new SerologyToAllelesMetadata(row.Locus, row.LookupName, mappings);
        }
    }
}