using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services.MatchingDictionary;
using System;
using System.Threading.Tasks;

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
        private readonly IAlleleNamesService alleleNamesService;

        public ManageMatchingDictionaryService(
            IHlaMatchingService matchingService, 
            IMatchingDictionaryRepository dictionaryRepository,
            IAlleleNamesService alleleNamesService)
        {
            this.matchingService = matchingService;
            this.dictionaryRepository = dictionaryRepository;
            this.alleleNamesService = alleleNamesService;
        }

        public async Task RecreateMatchingDictionary()
        {
            try
            {
                var allMatchedHla = matchingService.GetMatchedHla();
                var entries = allMatchedHla.ToMatchingDictionaryEntries();

                await RecreateAlleleNames();
                await dictionaryRepository.RecreateMatchingDictionaryTable(entries);
            }
            catch (Exception ex)
            {
                throw new MatchingDictionaryHttpException("Could not recreate the matching dictionary.", ex);
            }
        }

        /// <summary>
        /// The Allele Names collection must be recreated at the same time as the matching dictionary,
        /// so that lookups will have access to an up-to-date collection of Allele Names.
        /// </summary>
        /// <returns></returns>
        private async Task RecreateAlleleNames()
        {
            await alleleNamesService.RecreateAlleleNames();
        }
    }
}