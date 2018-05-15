using System.Collections.Generic;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda.Filters;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services.Dictionary;
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
            var allMatchedHla = GetMatchedHla();
            var entries = allMatchedHla.ToMatchingDictionaryEntries();
            dictionaryRepository.RecreateDictionaryTable(entries);
        }

        private IEnumerable<IMatchedHla> GetMatchedHla()
        {
            var alleleMatcher = new AlleleMatchingService(wmdaRepository);
            var serologyMatcher = new SerologyMatchingService(wmdaRepository);
            var hlaMatcher = new HlaMatchingService(wmdaRepository, alleleMatcher, serologyMatcher);

            return hlaMatcher.MatchAllHla(SerologyFilter.Instance.Filter, MolecularFilter.Instance.Filter);
        }
    }
}