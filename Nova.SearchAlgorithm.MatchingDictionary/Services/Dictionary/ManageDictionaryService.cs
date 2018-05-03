using Nova.SearchAlgorithm.MatchingDictionary.Repositories;

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
            var entries = new DictionaryGenerator().GenerateDictionaryEntries(wmdaRepository);
            dictionaryRepository.RecreateDictionaryTable(entries);
        }      
    }
}