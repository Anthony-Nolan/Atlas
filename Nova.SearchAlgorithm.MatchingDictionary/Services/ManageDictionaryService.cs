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
            var alleleMatcher = new AlleleMatchingService(wmdaRepository);
            var serologyMatcher = new SerologyMatchingService(wmdaRepository);

            var allMatchedHla = 
                new HlaMatchingService(wmdaRepository, alleleMatcher, serologyMatcher)
                    .MatchAllHla(SerologyFilter.Instance.Filter, MolecularFilter.Instance.Filter);

            dictionaryRepository.RecreateDictionaryTable(allMatchedHla);
        }
    }
}