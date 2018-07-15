using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using Nova.SearchAlgorithm.MatchingDictionary.Services.HlaMatchPreCalculation;

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
        private readonly IAlleleNamesService alleleNamesService;
        private readonly IHlaMatchingLookupResultGenerator hlaMatchingLookupResultGenerator;
        private readonly IHlaMatchingLookupRepository hlaMatchingLookupRepository;

        public ManageMatchingDictionaryService(
            IHlaMatchPreCalculationService matchPreCalculationService,
            IAlleleNamesService alleleNamesService,
            IHlaMatchingLookupResultGenerator hlaMatchingLookupResultGenerator,
            IHlaMatchingLookupRepository hlaMatchingLookupRepository)
        {
            this.matchPreCalculationService = matchPreCalculationService;
            this.alleleNamesService = alleleNamesService;
            this.hlaMatchingLookupResultGenerator = hlaMatchingLookupResultGenerator;
            this.hlaMatchingLookupRepository = hlaMatchingLookupRepository;
        }

        public async Task RecreateMatchingDictionary()
        {
            try
            {
                await RecreateHlaLookupData();
            }
            catch (Exception ex)
            {
                throw new MatchingDictionaryHttpException("Could not recreate the matching dictionary.", ex);
            }
        }

        private async Task RecreateHlaLookupData()
        {
            var precalculatedMatchedHla = matchPreCalculationService.GetMatchedHla();

            // Matching dictionary lookups require an up-to-date collection of allele names,
            // so both collections must be recreated together; the order of execution is not important.
            await Task.WhenAll(
                RecreateAlleleNames(),
                RecreateHlaMatchingLookupData(precalculatedMatchedHla)
            );
        }
        private async Task RecreateAlleleNames()
        {
            await alleleNamesService.RecreateAlleleNames();
        }

        private async Task RecreateHlaMatchingLookupData(IEnumerable<IMatchedHla> matchedHla)
        {
            var entries = hlaMatchingLookupResultGenerator.GetHlaMatchingLookupResults(matchedHla);
            await hlaMatchingLookupRepository.RecreateDataTable(entries);
        }
    }
}