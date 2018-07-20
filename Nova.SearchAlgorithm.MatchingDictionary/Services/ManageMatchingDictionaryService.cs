using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services.HlaDataConversion;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly IAlleleNamesService alleleNamesService;
        private readonly IHlaMatchingDataConverter hlaMatchingDataConverter;
        private readonly IHlaScoringDataConverter hlaScoringDataConverter;
        private readonly IHlaMatchingLookupRepository hlaMatchingLookupRepository;
        private readonly IHlaScoringLookupRepository hlaScoringLookupRepository;

        public ManageMatchingDictionaryService(
            IHlaMatchPreCalculationService matchPreCalculationService,
            IAlleleNamesService alleleNamesService,
            IHlaMatchingDataConverter hlaMatchingDataConverter,
            IHlaScoringDataConverter hlaScoringDataConverter,
            IHlaMatchingLookupRepository hlaMatchingLookupRepository,
            IHlaScoringLookupRepository hlaScoringLookupRepository)
        {
            this.matchPreCalculationService = matchPreCalculationService;
            this.alleleNamesService = alleleNamesService;
            this.hlaMatchingDataConverter = hlaMatchingDataConverter;
            this.hlaScoringDataConverter = hlaScoringDataConverter;
            this.hlaMatchingLookupRepository = hlaMatchingLookupRepository;
            this.hlaScoringLookupRepository = hlaScoringLookupRepository;
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
            var precalculatedMatchedHla = matchPreCalculationService
                .GetMatchedHla()
                .ToList();

            // Matching dictionary lookups require an up-to-date collection of allele names,
            // so both collections must be recreated together; the order of execution is not important.
            await Task.WhenAll(
                RecreateAlleleNames(),
                RecreateHlaMatchingLookupData(precalculatedMatchedHla),
                RecreateHlaScoringLookupData(precalculatedMatchedHla)
            );
        }
        private async Task RecreateAlleleNames()
        {
            await alleleNamesService.RecreateAlleleNames();
        }

        private async Task RecreateHlaMatchingLookupData(IEnumerable<IMatchedHla> matchedHla)
        {
            var hlaLookupResults = hlaMatchingDataConverter.ConvertToHlaLookupResults(matchedHla);
            await hlaMatchingLookupRepository.RecreateHlaLookupTable(hlaLookupResults);
        }

        private async Task RecreateHlaScoringLookupData(IEnumerable<IMatchedHla> matchedHla)
        {
            var hlaLookupResults = hlaScoringDataConverter.ConvertToHlaLookupResults(matchedHla);
            await hlaScoringLookupRepository.RecreateHlaLookupTable(hlaLookupResults);
        }
    }
}