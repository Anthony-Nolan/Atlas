using Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda.Filters;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services.Matching;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Dictionary
{
    public class DictionaryGenerator
    {
        public IEnumerable<MatchingDictionaryEntry> GenerateDictionaryEntries(IWmdaRepository wmdaRepository)
        {
            var allMatchedHla = GetAllMatchedHla(wmdaRepository).ToList();

            var entries = new List<MatchingDictionaryEntry>();
            entries.AddRange(GetDictionaryEntriesFromMatchedSerology(allMatchedHla.Where(m => !(m is MatchedAllele))));
            entries.AddRange(GetDictionaryEntriesFromMatchedAlleles(allMatchedHla.OfType<MatchedAllele>()));

            return entries;
        }

        private static IEnumerable<IMatchedHla> GetAllMatchedHla(IWmdaRepository wmdaRepository)
        {
            var alleleMatcher = new AlleleMatchingService(wmdaRepository);
            var serologyMatcher = new SerologyMatchingService(wmdaRepository);

            var allMatchedHla =
                new HlaMatchingService(wmdaRepository, alleleMatcher, serologyMatcher)
                    .MatchAllHla(SerologyFilter.Instance.Filter, MolecularFilter.Instance.Filter);
            return allMatchedHla;
        }

        private static IEnumerable<SerologyEntry> GetSerologyEntries(IEnumerable<Serology> serologyCollection)
        {
            return serologyCollection.Select(s => new SerologyEntry(s.Name, s.SerologySubtype));
        }

        private static IEnumerable<MatchingDictionaryEntry> GetDictionaryEntriesFromMatchedSerology(IEnumerable<IMatchedHla> matchedSerology)
        {
            return matchedSerology.Select(serology =>
                new MatchingDictionaryEntry(
                    serology.HlaType.MatchLocus,
                    serology.HlaType.Name,
                    TypingMethod.Serology,
                    MolecularSubtype.NotMolecularType,
                    ((Serology)serology.HlaType).SerologySubtype,
                    serology.MatchingPGroups,
                    GetSerologyEntries(serology.MatchingSerologies)
                ));
        }

        private static IEnumerable<MatchingDictionaryEntry> GetDictionaryEntriesFromMatchedAlleles(IEnumerable<MatchedAllele> matchedAlleles)
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
                    e.SelectMany(s => s.MatchingSerology).Distinct()
                    ));

            return grouped;
        }

        private static MatchingDictionaryEntry GetDictionaryEntryFromMatchedAllele(MatchedAllele matchedAllele, MolecularSubtype molecularSubtype)
        {
            var lookupName = GetAlleleLookupName((Allele)matchedAllele.HlaType, molecularSubtype);

            var entry = new MatchingDictionaryEntry(
                matchedAllele.HlaType.MatchLocus,
                lookupName,
                TypingMethod.Molecular,
                molecularSubtype,
                SerologySubtype.NotSerologyType,
                matchedAllele.MatchingPGroups,
                GetSerologyEntries(matchedAllele.MatchingSerologies));

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
