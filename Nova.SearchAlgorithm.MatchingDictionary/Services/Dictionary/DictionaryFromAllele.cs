using Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Dictionary
{
    public class DictionaryFromAllele
    {
        public IEnumerable<MatchingDictionaryEntry> GetDictionaryEntries(IEnumerable<IDictionaryAlleleSource> matchedAlleles)
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

        private static MatchingDictionaryEntry GetDictionaryEntryFromMatchedAllele(IDictionaryAlleleSource matchedAllele, MolecularSubtype molecularSubtype)
        {
            var lookupName = GetAlleleLookupName(matchedAllele.MatchedOnAllele, molecularSubtype);

            var entry = new MatchingDictionaryEntry(
                matchedAllele.MatchedOnAllele.MatchLocus,
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
