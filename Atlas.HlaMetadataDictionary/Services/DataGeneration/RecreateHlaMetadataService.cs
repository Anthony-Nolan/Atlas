using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.InternalExceptions;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration
{
    /// <summary>
    /// Manages the contents of the Metadata dictionary
    /// by orchestrating the generation and storage of the HLA Metadata dataset.
    /// </summary>
    internal interface IRecreateHlaMetadataService
    {
        Task RefreshAllHlaMetadata(string hlaNomenclatureVersion);
    }

    internal class RecreateHlaMetadataService : IRecreateHlaMetadataService
    {
        private readonly IHlaMetadataGenerationOrchestrator hlaMetadataGenerationOrchestrator;
        private readonly IAlleleNamesMetadataRepository alleleNamesMetadataRepository;
        private readonly IHlaMatchingMetadataRepository hlaMatchingMetadataRepository;
        private readonly IHlaScoringMetadataRepository hlaScoringMetadataRepository;
        private readonly IDpb1TceGroupsMetadataRepository dpb1TceGroupsMetadataRepository;
        private readonly ILogger logger;

        public RecreateHlaMetadataService(
            IHlaMetadataGenerationOrchestrator hlaMetadataGenerationOrchestrator,
            IAlleleNamesMetadataRepository alleleNamesMetadataRepository,
            IHlaMatchingMetadataRepository hlaMatchingMetadataRepository,
            IHlaScoringMetadataRepository hlaScoringMetadataRepository,
            IDpb1TceGroupsMetadataRepository dpb1TceGroupsMetadataRepository,
            ILogger logger)
        {
            this.hlaMetadataGenerationOrchestrator = hlaMetadataGenerationOrchestrator;
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
                logger.SendTrace("HlaMetadataDictionary: Fetching all Metadata");
                var allHlaMetadata = hlaMetadataGenerationOrchestrator.GenerateAllHlaMetadata(hlaNomenclatureVersion);
                
                logger.SendTrace("HlaMetadataDictionary: Persisting all Metadata");
                await PersistHlaMetadataCollection(allHlaMetadata, hlaNomenclatureVersion);
            }
            catch (Exception ex)
            {
                throw new HlaMetadataDictionaryHttpException("Could not recreate the HLA Metadata Dictionary.", ex);
            }
        }

        private async Task PersistHlaMetadataCollection(HlaMetadataCollection metadataCollection, string hlaNomenclatureVersion)
        {
            // Metadata Dictionary lookups require an up-to-date collection of allele names,
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