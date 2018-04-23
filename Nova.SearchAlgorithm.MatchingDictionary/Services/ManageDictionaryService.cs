using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda.Filters;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services.Matching;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
{
    public interface IManageDictionaryService
    {
        void RecreateDictionary(IWmdaRepository wmdaRepository);
    }
    public class ManageDictionaryService : IManageDictionaryService
    {
        private readonly IMatchedHlaRepository dictionaryRepository;

        public ManageDictionaryService(IMatchedHlaRepository repository)
        {
            dictionaryRepository = repository;
        }

        public void RecreateDictionary(IWmdaRepository wmdaRepository)
        {
            var alleleMatcher = new AlleleMatchingService(wmdaRepository);
            var serologyMatcher = new SerologyMatchingService(wmdaRepository);

            var allMatchedHla = 
                new HlaMatchingService(wmdaRepository, alleleMatcher, serologyMatcher)
                    .MatchAllHla(SerologyFilter.Instance.Filter, MolecularFilter.Instance.Filter);

            dictionaryRepository.RecreateDictionaryTable(allMatchedHla);
        }
    }
}