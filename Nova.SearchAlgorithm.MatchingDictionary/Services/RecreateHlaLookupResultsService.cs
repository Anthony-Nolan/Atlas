using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
{
    /// <summary>
    /// Manages the contents of the matching dictionary
    /// by orchestrating the generation and storage of the HLA Lookup Results dataset.
    /// </summary>
    public interface IRecreateHlaLookupResultsService
    {
        Task RecreateAllHlaLookupResults();
    }

    public class RecreateHlaLookupResultsService : IRecreateHlaLookupResultsService
    {
        private readonly IHlaLookupResultsService hlaLookupResultsService;
        private readonly IAlleleNamesLookupRepository alleleNamesLookupRepository;
        private readonly IHlaMatchingLookupRepository hlaMatchingLookupRepository;
        private readonly IHlaScoringLookupRepository hlaScoringLookupRepository;

        public RecreateHlaLookupResultsService(
            IHlaLookupResultsService hlaLookupResultsService,
            IAlleleNamesLookupRepository alleleNamesLookupRepository,
            IHlaMatchingLookupRepository hlaMatchingLookupRepository,
            IHlaScoringLookupRepository hlaScoringLookupRepository)
        {
            this.hlaLookupResultsService = hlaLookupResultsService;
            this.alleleNamesLookupRepository = alleleNamesLookupRepository;
            this.hlaMatchingLookupRepository = hlaMatchingLookupRepository;
            this.hlaScoringLookupRepository = hlaScoringLookupRepository;
        }

        public async Task RecreateAllHlaLookupResults()
        {
            try
            {
                var allHlaLookupResults = hlaLookupResultsService.GetAllHlaLookupResults();
                await PersistHlaLookupResultCollection(allHlaLookupResults);
            }
            catch (Exception ex)
            {
                throw new MatchingDictionaryHttpException("Could not recreate the matching dictionary.", ex);
            }
        }

        private async Task PersistHlaLookupResultCollection(HlaLookupResultCollections resultCollections)
        {
            // Matching dictionary lookups require an up-to-date collection of allele names,
            // so all collections must be recreated together; the order of execution is not important.

            // TODO: NOVA-1477 - Investigate why these tasks are not being executed in parallel.
            await Task.WhenAll(
                PersistAlleleNamesLookupResults(resultCollections.AlleleNameLookupResults),
                PersistHlaMatchingLookupResults(resultCollections.HlaMatchingLookupResults),
                PersistHlaScoringLookupResults(resultCollections.HlaScoringLookupResults)
            );            
        }

        private async Task PersistAlleleNamesLookupResults(IEnumerable<IHlaLookupResult> alleleNames)
        {
            await alleleNamesLookupRepository.RecreateHlaLookupTable(alleleNames);
        }

        private async Task PersistHlaMatchingLookupResults(IEnumerable<IHlaLookupResult> hlaLookupResults)
        {
            await hlaMatchingLookupRepository.RecreateHlaLookupTable(hlaLookupResults);
        }

        private async Task PersistHlaScoringLookupResults(IEnumerable<IHlaLookupResult> hlaLookupResults)
        {
            await hlaScoringLookupRepository.RecreateHlaLookupTable(hlaLookupResults);
        }
    }
}