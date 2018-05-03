using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda.Filters;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services.Matching;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
{
    public interface IManageDictionaryService
    {
        void RecreateDictionary();
    }
    public class ManageDictionaryService : IManageDictionaryService
    {
        private readonly IMatchedHlaRepository dictionaryRepository;
        private readonly IWmdaRepository wmdaRepository;

        public ManageDictionaryService(IMatchedHlaRepository dictionaryRepository, IWmdaRepository wmdaRepository)
        {
            this.dictionaryRepository = dictionaryRepository;
            this.wmdaRepository = wmdaRepository;
        }

        public void RecreateDictionary()
        {
            var allMatchedHla = GetAllMatchedHla().ToList();
            var entries = GetAllDictionaryEntries(allMatchedHla);

            dictionaryRepository.RecreateDictionaryTable(entries);
        }

        private IEnumerable<IMatchedHla> GetAllMatchedHla()
        {
            var alleleMatcher = new AlleleMatchingService(wmdaRepository);
            var serologyMatcher = new SerologyMatchingService(wmdaRepository);

            var allMatchedHla =
                new HlaMatchingService(wmdaRepository, alleleMatcher, serologyMatcher)
                    .MatchAllHla(SerologyFilter.Instance.Filter, MolecularFilter.Instance.Filter);
            return allMatchedHla;
        }

        private static IEnumerable<MatchingDictionaryEntry> GetAllDictionaryEntries(IReadOnlyCollection<IMatchedHla> allMatchedHla)
        {
            var entries = new List<MatchingDictionaryEntry>();
            entries.AddRange(GetDictionaryEntriesFromMatchedAlleles(allMatchedHla.OfType<MatchedAllele>()));
            entries.AddRange(GetDictionaryEntriesFromMatchedSerology(allMatchedHla.Where(m => !(m is MatchedAllele))));
            return entries;
        }

        private static IEnumerable<MatchingDictionaryEntry> GetDictionaryEntriesFromMatchedAlleles(IEnumerable<MatchedAllele> matchedAlleles)
        {
            var entries = new List<MatchingDictionaryEntry>(
                matchedAlleles.SelectMany(allele => new List<MatchingDictionaryEntry>{
                    allele.ToDictionaryEntry(MolecularSubtype.CompleteAllele),
                    allele.ToDictionaryEntry(MolecularSubtype.TwoFieldAllele),
                    allele.ToDictionaryEntry(MolecularSubtype.FirstFieldAllele)
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

        private static IEnumerable<MatchingDictionaryEntry> GetDictionaryEntriesFromMatchedSerology(IEnumerable<IMatchedHla> matchedSerology)
        {
            return matchedSerology.Select(serology =>
                serology.ToDictionaryEntry(((Serology)serology.HlaType).SerologySubtype));
        }
    }
}