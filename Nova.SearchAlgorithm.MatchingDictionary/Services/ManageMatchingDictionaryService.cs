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
        private readonly IHlaMatchPreCalculationService matchPreCalculationService;
        private readonly IPreCalculatedHlaMatchRepository preCalculatedHlaMatchRepository;
        private readonly IAlleleNamesService alleleNamesService;

        public ManageMatchingDictionaryService(
            IHlaMatchPreCalculationService matchPreCalculationService, 
            IPreCalculatedHlaMatchRepository preCalculatedHlaMatchRepository,
            IAlleleNamesService alleleNamesService)
        {
            this.matchPreCalculationService = matchPreCalculationService;
            this.preCalculatedHlaMatchRepository = preCalculatedHlaMatchRepository;
            this.alleleNamesService = alleleNamesService;
        }

        public async Task RecreateMatchingDictionary()
        {
            try
            {
                // Matching dictionary lookups require an up-to-date collection of allele names,
                // so both collections must be recreated together; the order of execution is not important.
                await Task.WhenAll(
                    RecreateAlleleNames(),
                    RecreatePreCalculatedHlaMatchInfo()
                    );
            }
            catch (Exception ex)
            {
                throw new MatchingDictionaryHttpException("Could not recreate the matching dictionary.", ex);
            }
        }

        private async Task RecreateAlleleNames()
        {
            await alleleNamesService.RecreateAlleleNames();
        }

        private async Task RecreatePreCalculatedHlaMatchInfo()
        {
            var allMatchedHla = matchPreCalculationService.GetMatchedHla();
            var entries = allMatchedHla.ToPreCalculatedHlaMatchInfo();
            await preCalculatedHlaMatchRepository.RecreatePreCalculatedHlaMatchesTable(entries);
        }
    }
}