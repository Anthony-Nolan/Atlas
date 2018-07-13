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
        private readonly IHlaMatchingLookupRepository hlaMatchingLookupRepository;
        private readonly IAlleleNamesService alleleNamesService;

        public ManageMatchingDictionaryService(
            IHlaMatchPreCalculationService matchPreCalculationService, 
            IHlaMatchingLookupRepository hlaMatchingLookupRepository,
            IAlleleNamesService alleleNamesService)
        {
            this.matchPreCalculationService = matchPreCalculationService;
            this.hlaMatchingLookupRepository = hlaMatchingLookupRepository;
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
                    RecreateHlaMatchingLookupData()
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

        private async Task RecreateHlaMatchingLookupData()
        {
            var allMatchedHla = matchPreCalculationService.GetMatchedHla();
            var entries = allMatchedHla.ToHlaMatchingLookupResult();
            await hlaMatchingLookupRepository.RecreateHlaMatchingLookupTable(entries);
        }
    }
}