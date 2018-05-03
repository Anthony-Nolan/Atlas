using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda.Filters;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services.Matching;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Dictionary
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
            var allMatchedHla = GetMatchedHla().ToList();
            var entries = GetDictionaryEntries(allMatchedHla);
            dictionaryRepository.RecreateDictionaryTable(entries);
        }

        private IEnumerable<IMatchedHla> GetMatchedHla()
        {
            var alleleMatcher = new AlleleMatchingService(wmdaRepository);
            var serologyMatcher = new SerologyMatchingService(wmdaRepository);
            var hlaMatcher = new HlaMatchingService(wmdaRepository, alleleMatcher, serologyMatcher);

            return hlaMatcher.MatchAllHla(SerologyFilter.Instance.Filter, MolecularFilter.Instance.Filter).ToList();
        }

        private static IEnumerable<MatchingDictionaryEntry> GetDictionaryEntries(IReadOnlyCollection<IMatchedHla> allMatchedHla)
        {
            var entries = new List<MatchingDictionaryEntry>();
            entries.AddRange(new DictionaryFromSerology().GetDictionaryEntries(allMatchedHla.Where(m => !(m is MatchedAllele))));
            entries.AddRange(new DictionaryFromAllele().GetDictionaryEntries(allMatchedHla.OfType<MatchedAllele>()));

            return entries;
        }
    }
}