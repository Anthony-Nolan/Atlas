using Nova.SearchAlgorithm.MatchingDictionary.Models.AlleleNames;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.HlaTypingInfo
{
    internal static class AlleleNamesSpecialCases
    {
        private static IEnumerable<AlleleNameEntry> AlleleNamesToRemove =>
            new List<AlleleNameEntry>
            {
                new AlleleNameEntry(MatchLocus.A, "30:100", new List<string>()),
                new AlleleNameEntry(MatchLocus.C, "02:16:01", new List<string>())
            };

        public static bool RemoveSpecifiedAlleleNames(AlleleNameEntry alleleName)
        {
            return AlleleNamesToRemove
                .Any(alleleNameToRemove => alleleNameToRemove.MatchLocusAndAlleleNameEquals(alleleName));
        }

        private static bool MatchLocusAndAlleleNameEquals(
            this AlleleNameEntry alleleName, AlleleNameEntry otherAlleleName)
        {
            return alleleName.MatchLocus.Equals(otherAlleleName.MatchLocus) &&
                   alleleName.LookupName.Equals(otherAlleleName.LookupName);
        }
    }
}
