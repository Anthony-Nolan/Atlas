using System.Threading.Tasks;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services.MatchingDictionary;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
{
    /// <summary>
    /// Manages the contents of the matching dictionary
    /// by orchestrating the generation and storage of the matched HLA dataset.
    /// </summary>
    public interface IManageMatchingDictionaryService
    {
        Task RecreateMatchingDictionary();
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

        public async Task RecreateMatchingDictionary()
        {
            var allMatchedHla = matchingService.GetMatchedHla();
            var entries = allMatchedHla.ToMatchingDictionaryEntries();
            await dictionaryRepository.RecreateMatchingDictionaryTable(entries);
        }
    }
}