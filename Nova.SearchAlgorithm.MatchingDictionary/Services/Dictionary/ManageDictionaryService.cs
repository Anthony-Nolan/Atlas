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
            var hlaMatcher = new HlaMatchingService(
                wmdaRepository, new AlleleMatchingService(wmdaRepository), new SerologyMatchingService(wmdaRepository));
            var allMatchedHla = hlaMatcher.MatchAllHla(SerologyFilter.Instance.Filter, MolecularFilter.Instance.Filter);

            var entries = new DictionaryGenerator().GenerateDictionaryEntries(allMatchedHla);
            dictionaryRepository.RecreateDictionaryTable(entries);
        }      
    }
}