using Atlas.HlaMetadataDictionary.Models.Wmda;
using System.Collections.Generic;
using System.Linq;
using Atlas.HlaMetadataDictionary.Models.Lookups.AlleleNameLookup;

namespace Atlas.HlaMetadataDictionary.Services.AlleleNames
{
    internal static class AlleleNameHistoryExtensions
    {
        public static IEnumerable<AlleleNameLookupResult> ToAlleleNameLookupResults(
            this AlleleNameHistory alleleNameHistory, string currentAlleleName)
        {
            return alleleNameHistory
                .DistinctAlleleNames
                .Select(name => new AlleleNameLookupResult(alleleNameHistory.TypingLocus, name, currentAlleleName));
        }
    }
}
