using Nova.SearchAlgorithm.MatchingDictionary.Models.AlleleNames;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.AlleleNames
{
    internal static class AlleleNameHistoryExtensions
    {
        public static IEnumerable<AlleleNameEntry> ToAlleleNameEntries(
            this AlleleNameHistory alleleNameHistory, string currentAlleleName)
        {
            return alleleNameHistory
                .DistinctAlleleNames
                .Select(name => new AlleleNameEntry(alleleNameHistory.Locus, name, currentAlleleName));
        }

        public static bool TryToAlleleNameEntries(
            this AlleleNameHistory alleleNameHistory, out IEnumerable<AlleleNameEntry> entries)
        {
            var currentNameIsNotNull = alleleNameHistory.CurrentAlleleName != null;

            entries = currentNameIsNotNull
                ? alleleNameHistory.ToAlleleNameEntries(alleleNameHistory.CurrentAlleleName)
                : new List<AlleleNameEntry>();

            return currentNameIsNotNull;
        }
    }
}
