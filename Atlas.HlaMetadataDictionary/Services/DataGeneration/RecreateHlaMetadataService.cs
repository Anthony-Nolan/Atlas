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
        private readonly IHlaMetadataService hlaMetadataService;
        private readonly IAlleleNamesMetadataRepository alleleNamesMetadataRepository;
        private readonly IHlaMatchingMetadataRepository hlaMatchingMetadataRepository;
        private readonly IHlaScoringMetadataRepository hlaScoringMetadataRepository;
        private readonly IDpb1TceGroupsMetadataRepository dpb1TceGroupsMetadataRepository;
        private readonly ILogger logger;

        public RecreateHlaMetadataService(
            IHlaMetadataService hlaMetadataService,
            IAlleleNamesMetadataRepository alleleNamesMetadataRepository,
            IHlaMatchingMetadataRepository hlaMatchingMetadataRepository,
            IHlaScoringMetadataRepository hlaScoringMetadataRepository,
            IDpb1TceGroupsMetadataRepository dpb1TceGroupsMetadataRepository,
            ILogger logger)
        {
            this.hlaMetadataService = hlaMetadataService;
            this.alleleNamesMetadataRepository = alleleNamesMetadataRepository;
            this.hlaMatchingMetadataRepository = hlaMatchingMetadataRepository;
            this.hlaScoringMetadataRepository = hlaScoringMetadataRepository;
            this.dpb1TceGroupsMetadataRepository = dpb1TceGroupsMetadataRepository;
            this.logger = logger;
        }

        public async Task RefreshAllHlaMetadata(string hlaNomenclatureVersion)
        {
            try
            {
                logger.SendTrace("HlaMetadataDictionary: Fetching all lookup results", LogLevel.Info);
                var allHlaMetadata = hlaMetadataService.GetAllHlaMetadata(hlaNomenclatureVersion);
                
                logger.SendTrace("HlaMetadataDictionary: Persisting lookup results", LogLevel.Info);
                await PersistHlaMetadataCollection(allHlaMetadata, hlaNomenclatureVersion);
            }
            catch (Exception ex)
            {
                throw new HlaMetadataDictionaryHttpException("Could not recreate the matching dictionary.", ex);
            }
        }

        private async Task PersistHlaMetadataCollection(HlaMetadataCollection metadataCollection, string hlaNomenclatureVersion)
        {
            // Matching dictionary lookups require an up-to-date collection of allele names,
            // so all collections must be recreated together; the order of execution is not important.
            await Task.WhenAll(
                PersistAlleleNamesMetadata(metadataCollection.AlleleNameMetadata, hlaNomenclatureVersion),
                PersistHlaMatchingMetadata(metadataCollection.HlaMatchingMetadata, hlaNomenclatureVersion),
                PersistHlaScoringMetadata(metadataCollection.HlaScoringMetadata, hlaNomenclatureVersion),
                PersistDpb1TceGroupMetadata(metadataCollection.Dpb1TceGroupMetadata, hlaNomenclatureVersion)
            );
        }

        private async Task PersistAlleleNamesMetadata(IEnumerable<ISerialisableHlaMetadata> alleleNames, string hlaNomenclatureVersion)
        {
            await alleleNamesMetadataRepository.RecreateHlaMetadataTable(alleleNames, hlaNomenclatureVersion);
        }

        private async Task PersistHlaMatchingMetadata(IEnumerable<ISerialisableHlaMetadata> hlaMatchingMetadata, string hlaNomenclatureVersion)
        {
            await hlaMatchingMetadataRepository.RecreateHlaMetadataTable(hlaMatchingMetadata, hlaNomenclatureVersion);
        }

        private async Task PersistHlaScoringMetadata(IEnumerable<ISerialisableHlaMetadata> hlaScoringMetadata, string hlaNomenclatureVersion)
        {
            await hlaScoringMetadataRepository.RecreateHlaMetadataTable(hlaScoringMetadata, hlaNomenclatureVersion);
        }

        private async Task PersistDpb1TceGroupMetadata(IEnumerable<ISerialisableHlaMetadata> dpb1TceGroupMetadata, string hlaNomenclatureVersion)
        {
            await dpb1TceGroupsMetadataRepository.RecreateHlaMetadataTable(dpb1TceGroupMetadata, hlaNomenclatureVersion);
        }
    }
}