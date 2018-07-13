using Nova.SearchAlgorithm.MatchingDictionary.Models.AlleleNames;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.AlleleNames
{
    internal static class AlleleNameHistoryExtensions
    {
        public static IEnumerable<AlleleNameLookupResult> ToAlleleNameLookupResults(
            this AlleleNameHistory alleleNameHistory, string currentAlleleName)
        {
            return alleleNameHistory
                .DistinctAlleleNames
                .Select(name => new AlleleNameLookupResult(alleleNameHistory.Locus, name, currentAlleleName));
        }
    }
}
