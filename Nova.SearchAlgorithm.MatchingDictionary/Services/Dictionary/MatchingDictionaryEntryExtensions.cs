using Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Dictionary
{
    /// <summary>
    /// Converts matched HLA objects to dictionary entry objects 
    /// so the data is optimally modeled for matching dictionary lookups.
    /// </summary>
    public static class MatchingDictionaryEntryExtensions
    {
        public static IEnumerable<MatchingDictionaryEntry> ToMatchingDictionaryEntries(this IEnumerable<IMatchedHla> matchedHla)
        {
            var hla = matchedHla.ToArray();

            var entries = new List<MatchingDictionaryEntry>();
            entries.AddRange(GetDictionaryEntriesFromSerology(hla.OfType<IDictionarySource<SerologyTyping>>()));
            entries.AddRange(GetDictionaryEntriesFromAlleles(hla.OfType<IDictionarySource<AlleleTyping>>()));

            return entries;
        }

        private static IEnumerable<MatchingDictionaryEntry> GetDictionaryEntriesFromSerology(IEnumerable<IDictionarySource<SerologyTyping>> matchedSerology)
        {
            return matchedSerology.Select(serology =>
                new MatchingDictionaryEntry(
                    serology.TypingForDictionary.MatchLocus,
                    serology.TypingForDictionary.Name,
                    TypingMethod.Serology,
                    MolecularSubtype.NotMolecularTyping,
                    serology.TypingForDictionary.SerologySubtype,
                    serology.MatchingPGroups,
                    serology.MatchingSerologies.ToSerologyEntries()
                ));
        }

        private static IEnumerable<MatchingDictionaryEntry> GetDictionaryEntriesFromAlleles(IEnumerable<IDictionarySource<AlleleTyping>> matchedAlleles)
        {
            var entries = new List<MatchingDictionaryEntry>(
                matchedAlleles.SelectMany(allele => new List<MatchingDictionaryEntry>{
                    GetDictionaryEntryFromMatchedAllele(allele, MolecularSubtype.CompleteAllele),
                    GetDictionaryEntryFromMatchedAllele(allele, MolecularSubtype.TwoFieldAllele),
                    GetDictionaryEntryFromMatchedAllele(allele, MolecularSubtype.FirstFieldAllele)
                    }));

            var grouped = entries
                .GroupBy(e => new { e.MatchLocus, e.LookupName, e.TypingMethod })
                .Select(e => new MatchingDictionaryEntry(
                    e.Key.MatchLocus,
                    e.Key.LookupName,
                    e.Key.TypingMethod,
                    e.Select(m => m.MolecularSubtype).OrderBy(m => m).First(),
                    SerologySubtype.NotSerologyTyping,
                    e.SelectMany(p => p.MatchingPGroups).Distinct(),
                    e.SelectMany(s => s.MatchingSerologies).Distinct()
                    ));

            return grouped;
        }

        private static MatchingDictionaryEntry GetDictionaryEntryFromMatchedAllele(IDictionarySource<AlleleTyping> matchedAllele, MolecularSubtype molecularSubtype)
        {
            var lookupName = GetAlleleLookupName(matchedAllele.TypingForDictionary, molecularSubtype);

            var entry = new MatchingDictionaryEntry(
                matchedAllele.TypingForDictionary.MatchLocus,
                lookupName,
                TypingMethod.Molecular,
                molecularSubtype,
                SerologySubtype.NotSerologyTyping,
                matchedAllele.MatchingPGroups,
                matchedAllele.MatchingSerologies.ToSerologyEntries()
                );

            return entry;
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
