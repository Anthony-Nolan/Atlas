using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
{
    /// <summary>
    /// Manages the contents of the matching dictionary
    /// by orchestrating the generation and storage of the HLA Lookup Results dataset.
    /// </summary>
    public interface IRecreateHlaLookupResultsService
    {
        Task RecreateAllHlaLookupResults(string hlaDatabaseVersion);
    }

    public class RecreateHlaLookupResultsService : IRecreateHlaLookupResultsService
    {
        private readonly IHlaLookupResultsService hlaLookupResultsService;
        private readonly IAlleleNamesLookupRepository alleleNamesLookupRepository;
        private readonly IHlaMatchingLookupRepository hlaMatchingLookupRepository;
        private readonly IHlaScoringLookupRepository hlaScoringLookupRepository;
        private readonly IDpb1TceGroupsLookupRepository dpb1TceGroupsLookupRepository;

        public RecreateHlaLookupResultsService(
            IHlaLookupResultsService hlaLookupResultsService,
            IAlleleNamesLookupRepository alleleNamesLookupRepository,
            IHlaMatchingLookupRepository hlaMatchingLookupRepository,
            IHlaScoringLookupRepository hlaScoringLookupRepository,
            IDpb1TceGroupsLookupRepository dpb1TceGroupsLookupRepository)
        {
            this.hlaLookupResultsService = hlaLookupResultsService;
            this.alleleNamesLookupRepository = alleleNamesLookupRepository;
            this.hlaMatchingLookupRepository = hlaMatchingLookupRepository;
            this.hlaScoringLookupRepository = hlaScoringLookupRepository;
            this.dpb1TceGroupsLookupRepository = dpb1TceGroupsLookupRepository;
        }

        public async Task RecreateAllHlaLookupResults(string hlaDatabaseVersion)
        {
            try
            {
                var allHlaLookupResults = hlaLookupResultsService.GetAllHlaLookupResults(hlaDatabaseVersion);
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
            await Task.WhenAll(
                PersistAlleleNamesLookupResults(resultCollections.AlleleNameLookupResults),
                PersistHlaMatchingLookupResults(resultCollections.HlaMatchingLookupResults),
                PersistHlaScoringLookupResults(resultCollections.HlaScoringLookupResults),
                PersistDpb1TceGroupLookupResults(resultCollections.Dpb1TceGroupLookupResults)
            );
        }

        private async Task PersistAlleleNamesLookupResults(IEnumerable<IHlaLookupResult> alleleNames)
        {
            await alleleNamesLookupRepository.RecreateHlaLookupTable(alleleNames);
        }

        private async Task PersistHlaMatchingLookupResults(IEnumerable<IHlaLookupResult> hlaMatchingLookupResults)
        {
            await hlaMatchingLookupRepository.RecreateHlaLookupTable(hlaMatchingLookupResults);
        }

        private async Task PersistHlaScoringLookupResults(IEnumerable<IHlaLookupResult> hlaScoringLookupResults)
        {
            await hlaScoringLookupRepository.RecreateHlaLookupTable(hlaScoringLookupResults);
        }

        private async Task PersistDpb1TceGroupLookupResults(IEnumerable<IHlaLookupResult> dpb1TceGroupLookupResults)
        {
            await dpb1TceGroupsLookupRepository.RecreateHlaLookupTable(dpb1TceGroupLookupResults);
        }
    }
}