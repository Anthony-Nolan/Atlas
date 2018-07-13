using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary.MatchingLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.MatchingDictionary
{
    /// <summary>
    /// Optimises data in matched HLA objects for HLA matching lookups.
    /// </summary>
    public static class MatchedHlaExtensions
    {
        public static IEnumerable<HlaMatchingLookupResult> ToHlaMatchingLookupResult(this IEnumerable<IMatchedHla> matchedHla)
        {
            var matchedHlaList = matchedHla.ToList();

            var entries = new List<HlaMatchingLookupResult>();
            entries.AddRange(GetHlaMatchingLookupResultFromSerologies(matchedHlaList.OfType<IMatchingDictionarySource<SerologyTyping>>()));
            entries.AddRange(GetHlaMatchingLookupResultFromAlleles(matchedHlaList.OfType<IMatchingDictionarySource<AlleleTyping>>()));

            return entries;
        }

        private static IEnumerable<HlaMatchingLookupResult> GetHlaMatchingLookupResultFromSerologies(
            IEnumerable<IMatchingDictionarySource<SerologyTyping>> matchedSerology)
        {
            return matchedSerology.Select(serology => new HlaMatchingLookupResult(serology));
        }

        private static IEnumerable<HlaMatchingLookupResult> GetHlaMatchingLookupResultFromAlleles(
            IEnumerable<IMatchingDictionarySource<AlleleTyping>> matchedAlleles)
        {
            var entries = matchedAlleles.SelectMany(GetHlaMatchingLookupResultsForEachAlleleLookupName);

            var groupedResultsToMergeDuplicatesCausedByAlleleNameTruncation = entries
                .GroupBy(e => new { e.MatchLocus, e.LookupName })
                .Select(e => new HlaMatchingLookupResult(
                    e.Key.MatchLocus,
                    e.Key.LookupName,
                    TypingMethod.Molecular,
                    e.SelectMany(p => p.MatchingPGroups).Distinct()
                ));

            return groupedResultsToMergeDuplicatesCausedByAlleleNameTruncation;
        }

        private static IEnumerable<HlaMatchingLookupResult> GetHlaMatchingLookupResultsForEachAlleleLookupName(
            IMatchingDictionarySource<AlleleTyping> matchedAllele)
        {
            var lookupNames = GetAlleleLookupNames(matchedAllele.TypingForMatchingDictionary);

            return lookupNames.Select(lookupName => new HlaMatchingLookupResult(matchedAllele, lookupName));
        }

        private static IEnumerable<string> GetAlleleLookupNames(AlleleTyping alleleTyping)
        {
            return new List<string>
            {
                alleleTyping.Name,
                alleleTyping.TwoFieldName,
                alleleTyping.Fields.ElementAt(0)
            };
        }
    }
}
