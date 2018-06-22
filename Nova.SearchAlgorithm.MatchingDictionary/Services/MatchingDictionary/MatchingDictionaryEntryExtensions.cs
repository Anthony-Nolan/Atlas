using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.MatchingDictionary
{
    /// <summary>
    /// Converts matched HLA objects to dictionary entry objects 
    /// so the data is optimally modeled for matching dictionary lookups.
    /// </summary>
    public static class MatchingDictionaryEntryExtensions
    {
        public static IEnumerable<MatchingDictionaryEntry> ToMatchingDictionaryEntries(this IEnumerable<IMatchedHla> matchedHla)
        {
            var matchedHlaList = matchedHla.ToList();

            var entries = new List<MatchingDictionaryEntry>();
            entries.AddRange(GetMatchingDictionaryEntriesFromSerology(matchedHlaList.OfType<IMatchingDictionarySource<SerologyTyping>>()));
            entries.AddRange(GetMatchingDictionaryEntriesFromAlleles(matchedHlaList.OfType<IMatchingDictionarySource<AlleleTyping>>()));

            return entries;
        }

        private static IEnumerable<MatchingDictionaryEntry> GetMatchingDictionaryEntriesFromSerology(IEnumerable<IMatchingDictionarySource<SerologyTyping>> matchedSerology)
        {
            return matchedSerology.Select(serology => new MatchingDictionaryEntry(serology));
        }

        private static IEnumerable<MatchingDictionaryEntry> GetMatchingDictionaryEntriesFromAlleles(IEnumerable<IMatchingDictionarySource<AlleleTyping>> matchedAlleles)
        {
            var entries = matchedAlleles.SelectMany(GetMatchingDictionaryEntriesForEachMolecularSubtype);

            var groupByQueryToMergeDuplicateEntriesCausedByAlleleNameTruncation = entries
                .GroupBy(e => new { e.MatchLocus, e.LookupName })
                .Select(e => new MatchingDictionaryEntry(
                    e.Key.MatchLocus,
                    e.Key.LookupName,
                    TypingMethod.Molecular,
                    e.Select(m => m.MolecularSubtype).OrderBy(m => m).First(),
                    SerologySubtype.NotSerologyTyping,
                    e.Select(m => m.AlleleTypingStatus).First(),
                    e.SelectMany(p => p.MatchingPGroups).Distinct(),
                    e.SelectMany(g => g.MatchingGGroups).Distinct(),
                    e.SelectMany(s => s.MatchingSerologies).Distinct()
                ));

            return groupByQueryToMergeDuplicateEntriesCausedByAlleleNameTruncation;
        }

        private static IEnumerable<MatchingDictionaryEntry> GetMatchingDictionaryEntriesForEachMolecularSubtype(
            IMatchingDictionarySource<AlleleTyping> matchedAllele)
        {
            var entries = new List<MatchingDictionaryEntry>
            {
                GetMatchingDictionaryEntryFromMatchedAllele(matchedAllele, MolecularSubtype.CompleteAllele),
                GetMatchingDictionaryEntryFromMatchedAllele(matchedAllele, MolecularSubtype.TwoFieldAllele),
                GetMatchingDictionaryEntryFromMatchedAllele(matchedAllele, MolecularSubtype.FirstFieldAllele)
            };

            return entries;
        }

        private static MatchingDictionaryEntry GetMatchingDictionaryEntryFromMatchedAllele(IMatchingDictionarySource<AlleleTyping> matchedAllele, MolecularSubtype molecularSubtype)
        {
            var lookupName = GetAlleleLookupName(matchedAllele.TypingForMatchingDictionary, molecularSubtype);
            return new MatchingDictionaryEntry(matchedAllele, lookupName, molecularSubtype);
        }

        private static string GetAlleleLookupName(AlleleTyping alleleTyping, MolecularSubtype molecularSubtype)
        {
            switch (molecularSubtype)
            {
                case MolecularSubtype.CompleteAllele:
                    return alleleTyping.Name;
                case MolecularSubtype.TwoFieldAllele:
                    return alleleTyping.TwoFieldName;
                case MolecularSubtype.FirstFieldAllele:
                    return alleleTyping.Fields.ElementAt(0);
                default:
                    return string.Empty;
            }
        }
    }
}
