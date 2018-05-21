using Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Dictionary
{
    /// <summary>
    /// This class is responsible for
    /// converting matched HLA to dictionary entry objects 
    /// that are optimised for the dictionary lookup requirements.
    /// </summary>
    public static class MatchingDictionaryEntryExtensions
    {
        public static IEnumerable<MatchingDictionaryEntry> ToMatchingDictionaryEntries(this IEnumerable<IMatchedHla> matchedHla)
        {
            var hla = matchedHla.ToArray();

            var entries = new List<MatchingDictionaryEntry>();
            entries.AddRange(GetDictionaryEntriesFromSerology(hla.OfType<IDictionarySource<Serology>>()));
            entries.AddRange(GetDictionaryEntriesFromAlleles(hla.OfType<IDictionarySource<Allele>>()));

            return entries;
        }

        private static IEnumerable<MatchingDictionaryEntry> GetDictionaryEntriesFromSerology(IEnumerable<IDictionarySource<Serology>> matchedSerology)
        {
            return matchedSerology.Select(serology =>
                new MatchingDictionaryEntry(
                    serology.TypeForDictionary.MatchLocus,
                    serology.TypeForDictionary.Name,
                    TypingMethod.Serology,
                    MolecularSubtype.NotMolecularType,
                    serology.TypeForDictionary.SerologySubtype,
                    serology.MatchingPGroups,
                    serology.MatchingSerologies.ToSerologyEntries()
                ));
        }

        private static IEnumerable<MatchingDictionaryEntry> GetDictionaryEntriesFromAlleles(IEnumerable<IDictionarySource<Allele>> matchedAlleles)
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
                    SerologySubtype.NotSerologyType,
                    e.SelectMany(p => p.MatchingPGroups).Distinct(),
                    e.SelectMany(s => s.MatchingSerologies).Distinct()
                    ));

            return grouped;
        }

        private static MatchingDictionaryEntry GetDictionaryEntryFromMatchedAllele(IDictionarySource<Allele> matchedAllele, MolecularSubtype molecularSubtype)
        {
            var lookupName = GetAlleleLookupName(matchedAllele.TypeForDictionary, molecularSubtype);

            var entry = new MatchingDictionaryEntry(
                matchedAllele.TypeForDictionary.MatchLocus,
                lookupName,
                TypingMethod.Molecular,
                molecularSubtype,
                SerologySubtype.NotSerologyType,
                matchedAllele.MatchingPGroups,
                matchedAllele.MatchingSerologies.ToSerologyEntries()
                );

            return entry;
        }

        private static string GetAlleleLookupName(Allele allele, MolecularSubtype molecularSubtype)
        {
            switch (molecularSubtype)
            {
                case MolecularSubtype.CompleteAllele:
                    return allele.Name;
                case MolecularSubtype.TwoFieldAllele:
                    return allele.TwoFieldName;
                case MolecularSubtype.FirstFieldAllele:
                    return allele.Fields.ElementAt(0);
                default:
                    return string.Empty;
            }
        }
    }
}
