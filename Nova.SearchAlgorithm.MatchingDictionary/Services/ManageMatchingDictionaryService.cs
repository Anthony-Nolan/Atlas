using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda.Filters;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services.Dictionary;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
{
    /// <summary>
    /// Manages the contents of the matching dictionary
    /// by orchestrating the generation and storage of the matched HLA dataset.
    /// </summary>
    public interface IManageMatchingDictionaryService
    {
        void RecreateDictionary();
    }

    public class ManageMatchingDictionaryService : IManageMatchingDictionaryService
    {
        private readonly IHlaMatchingService matchingService;
        private readonly IMatchingDictionaryRepository dictionaryRepository;

        public ManageMatchingDictionaryService(IHlaMatchingService matchingService, IMatchingDictionaryRepository dictionaryRepository)
        {
            this.matchingService = matchingService;
            this.dictionaryRepository = dictionaryRepository;
        }

        public void RecreateDictionary()
        {
            var allMatchedHla = matchingService.GetMatchedHla(SerologyFilter.Instance.Filter, MolecularFilter.Instance.Filter);
            var entries = allMatchedHla.ToMatchingDictionaryEntries();
            dictionaryRepository.RecreateMatchingDictionaryTable(entries);
        }
    }
}