using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.Models.Lookups.AlleleNameLookup;
using Atlas.HlaMetadataDictionary.Models.Wmda;

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
