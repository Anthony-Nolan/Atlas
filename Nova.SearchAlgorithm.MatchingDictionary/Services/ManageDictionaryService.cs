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
        private readonly IMatchedHlaRepository dictionaryRepo;

        public ManageDictionaryService(IMatchedHlaRepository repository)
        {
            dictionaryRepo = repository;
        }

        public void RecreateDictionary()
        {
            var wmdaRepo = WmdaRepository.Instance;
            var alleleMatcher = new AlleleMatchingService(wmdaRepo);
            var serologyMatcher = new SerologyMatchingService(wmdaRepo);

            var allMatchedHla = 
                new HlaMatchingService(wmdaRepo, alleleMatcher, serologyMatcher)
                    .MatchAllHla(SerologyFilter.Instance.Filter, MolecularFilter.Instance.Filter);

            dictionaryRepo.RecreateDictionaryTable(allMatchedHla);
        }
    }
}