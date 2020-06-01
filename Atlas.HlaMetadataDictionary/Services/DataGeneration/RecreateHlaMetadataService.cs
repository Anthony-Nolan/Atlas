using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.Exceptions;
using Atlas.HlaMetadataDictionary.Models.Lookups;
using Atlas.HlaMetadataDictionary.Repositories.LookupRepositories;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration
{
    /// <summary>
    /// Manages the contents of the matching dictionary
    /// by orchestrating the generation and storage of the HLA Lookup Results dataset.
    /// </summary>
    internal interface IRecreateHlaMetadataService
    {
        Task RefreshAllHlaMetadata(string hlaNomenclatureVersion);
    }

    internal class RecreateHlaMetadataService : IRecreateHlaMetadataService
    {
        private readonly IHlaLookupResultsService hlaLookupResultsService;
        private readonly IAlleleNamesLookupRepository alleleNamesLookupRepository;
        private readonly IHlaMatchingLookupRepository hlaMatchingLookupRepository;
        private readonly IHlaScoringLookupRepository hlaScoringLookupRepository;
        private readonly IDpb1TceGroupsLookupRepository dpb1TceGroupsLookupRepository;
        private readonly ILogger logger;

        public RecreateHlaMetadataService(
            IHlaLookupResultsService hlaLookupResultsService,
            IAlleleNamesLookupRepository alleleNamesLookupRepository,
            IHlaMatchingLookupRepository hlaMatchingLookupRepository,
            IHlaScoringLookupRepository hlaScoringLookupRepository,
            IDpb1TceGroupsLookupRepository dpb1TceGroupsLookupRepository,
            ILogger logger)
        {
            this.hlaLookupResultsService = hlaLookupResultsService;
            this.alleleNamesLookupRepository = alleleNamesLookupRepository;
            this.hlaMatchingLookupRepository = hlaMatchingLookupRepository;
            this.hlaScoringLookupRepository = hlaScoringLookupRepository;
            this.dpb1TceGroupsLookupRepository = dpb1TceGroupsLookupRepository;
            this.logger = logger;
        }

        public async Task RefreshAllHlaMetadata(string hlaNomenclatureVersion)
        {
            try
            {
                logger.SendTrace("HlaMetadataDictionary: Fetching all lookup results", LogLevel.Info);
                var allHlaLookupResults = hlaLookupResultsService.GetAllHlaLookupResults(hlaNomenclatureVersion);
                
                logger.SendTrace("HlaMetadataDictionary: Persisting lookup results", LogLevel.Info);
                await PersistHlaLookupResultCollection(allHlaLookupResults, hlaNomenclatureVersion);
            }
            catch (Exception ex)
            {
                throw new HlaMetadataDictionaryHttpException("Could not recreate the matching dictionary.", ex);
            }
        }

        private async Task PersistHlaLookupResultCollection(HlaLookupResultCollections resultCollections, string hlaNomenclatureVersion)
        {
            // Matching dictionary lookups require an up-to-date collection of allele names,
            // so all collections must be recreated together; the order of execution is not important.
            await Task.WhenAll(
                PersistAlleleNamesLookupResults(resultCollections.AlleleNameLookupResults, hlaNomenclatureVersion),
                PersistHlaMatchingLookupResults(resultCollections.HlaMatchingLookupResults, hlaNomenclatureVersion),
                PersistHlaScoringLookupResults(resultCollections.HlaScoringLookupResults, hlaNomenclatureVersion),
                PersistDpb1TceGroupLookupResults(resultCollections.Dpb1TceGroupLookupResults, hlaNomenclatureVersion)
            );
        }

        private async Task PersistAlleleNamesLookupResults(IEnumerable<ISerialisableHlaMetadata> alleleNames, string hlaNomenclatureVersion)
        {
            await alleleNamesLookupRepository.RecreateHlaLookupTable(alleleNames, hlaNomenclatureVersion);
        }

        private async Task PersistHlaMatchingLookupResults(IEnumerable<ISerialisableHlaMetadata> hlaMatchingLookupResults, string hlaNomenclatureVersion)
        {
            await hlaMatchingLookupRepository.RecreateHlaLookupTable(hlaMatchingLookupResults, hlaNomenclatureVersion);
        }

        private async Task PersistHlaScoringLookupResults(IEnumerable<ISerialisableHlaMetadata> hlaScoringLookupResults, string hlaNomenclatureVersion)
        {
            await hlaScoringLookupRepository.RecreateHlaLookupTable(hlaScoringLookupResults, hlaNomenclatureVersion);
        }

        private async Task PersistDpb1TceGroupLookupResults(IEnumerable<ISerialisableHlaMetadata> dpb1TceGroupLookupResults, string hlaNomenclatureVersion)
        {
            await dpb1TceGroupsLookupRepository.RecreateHlaLookupTable(dpb1TceGroupLookupResults, hlaNomenclatureVersion);
        }
    }
}