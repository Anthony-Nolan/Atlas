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
                await PersistHlaLookupResultCollection(allHlaLookupResults, hlaDatabaseVersion);
            }
            catch (Exception ex)
            {
                throw new MatchingDictionaryHttpException("Could not recreate the matching dictionary.", ex);
            }
        }

        private async Task PersistHlaLookupResultCollection(HlaLookupResultCollections resultCollections, string hlaDatabaseVersion)
        {
            // Matching dictionary lookups require an up-to-date collection of allele names,
            // so all collections must be recreated together; the order of execution is not important.
            await Task.WhenAll(
                PersistAlleleNamesLookupResults(resultCollections.AlleleNameLookupResults, hlaDatabaseVersion),
                PersistHlaMatchingLookupResults(resultCollections.HlaMatchingLookupResults, hlaDatabaseVersion),
                PersistHlaScoringLookupResults(resultCollections.HlaScoringLookupResults, hlaDatabaseVersion),
                PersistDpb1TceGroupLookupResults(resultCollections.Dpb1TceGroupLookupResults, hlaDatabaseVersion)
            );
        }

        private async Task PersistAlleleNamesLookupResults(IEnumerable<IHlaLookupResult> alleleNames, string hlaDatabaseVersion)
        {
            await alleleNamesLookupRepository.RecreateHlaLookupTable(alleleNames, hlaDatabaseVersion);
        }

        private async Task PersistHlaMatchingLookupResults(IEnumerable<IHlaLookupResult> hlaMatchingLookupResults, string hlaDatabaseVersion)
        {
            await hlaMatchingLookupRepository.RecreateHlaLookupTable(hlaMatchingLookupResults, hlaDatabaseVersion);
        }

        private async Task PersistHlaScoringLookupResults(IEnumerable<IHlaLookupResult> hlaScoringLookupResults, string hlaDatabaseVersion)
        {
            await hlaScoringLookupRepository.RecreateHlaLookupTable(hlaScoringLookupResults, hlaDatabaseVersion);
        }

        private async Task PersistDpb1TceGroupLookupResults(IEnumerable<IHlaLookupResult> dpb1TceGroupLookupResults, string hlaDatabaseVersion)
        {
            await dpb1TceGroupsLookupRepository.RecreateHlaLookupTable(dpb1TceGroupLookupResults, hlaDatabaseVersion);
        }
    }
}