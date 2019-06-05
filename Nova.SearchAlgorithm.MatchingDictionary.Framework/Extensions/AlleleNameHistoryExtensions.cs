using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.AlleleNameLookup;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.AlleleNames
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
