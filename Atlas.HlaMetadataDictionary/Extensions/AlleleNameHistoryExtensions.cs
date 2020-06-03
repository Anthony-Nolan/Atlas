using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;
using Atlas.HlaMetadataDictionary.WmdaDataAccess.Models;

namespace Atlas.HlaMetadataDictionary.Extensions
{
    internal static class AlleleNameHistoryExtensions
    {
        public static IEnumerable<AlleleNameMetadata> ToAlleleNameMetadata(
            this AlleleNameHistory alleleNameHistory, string currentAlleleName)
        {
            return alleleNameHistory
                .DistinctAlleleNames
                .Select(name => new AlleleNameMetadata(alleleNameHistory.TypingLocus, name, currentAlleleName));
        }
    }
}
