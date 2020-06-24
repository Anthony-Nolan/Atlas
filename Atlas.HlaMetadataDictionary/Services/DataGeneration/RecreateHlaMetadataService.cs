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
        private readonly IAlleleGroupsMetadataRepository alleleGroupsMetadataRepository;
        private readonly ILogger logger;

        public RecreateHlaMetadataService(
            IHlaMetadataGenerationOrchestrator hlaMetadataGenerationOrchestrator,
            IAlleleNamesMetadataRepository alleleNamesMetadataRepository,
            IHlaMatchingMetadataRepository hlaMatchingMetadataRepository,
            IHlaScoringMetadataRepository hlaScoringMetadataRepository,
            IDpb1TceGroupsMetadataRepository dpb1TceGroupsMetadataRepository,
            IAlleleGroupsMetadataRepository alleleGroupsMetadataRepository,
            ILogger logger)
        {
            this.hlaMetadataGenerationOrchestrator = hlaMetadataGenerationOrchestrator;
            this.alleleNamesMetadataRepository = alleleNamesMetadataRepository;
            this.hlaMatchingMetadataRepository = hlaMatchingMetadataRepository;
            this.hlaScoringMetadataRepository = hlaScoringMetadataRepository;
            this.dpb1TceGroupsMetadataRepository = dpb1TceGroupsMetadataRepository;
            this.alleleGroupsMetadataRepository = alleleGroupsMetadataRepository;
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
                PersistMetadata(alleleNamesMetadataRepository, metadataCollection.AlleleNameMetadata, hlaNomenclatureVersion),
                PersistMetadata(hlaMatchingMetadataRepository, metadataCollection.HlaMatchingMetadata, hlaNomenclatureVersion),
                PersistMetadata(hlaScoringMetadataRepository, metadataCollection.HlaScoringMetadata, hlaNomenclatureVersion),
                PersistMetadata(dpb1TceGroupsMetadataRepository, metadataCollection.Dpb1TceGroupMetadata, hlaNomenclatureVersion),
                PersistMetadata(alleleGroupsMetadataRepository, metadataCollection.AlleleGroupMetadata, hlaNomenclatureVersion)
            );
        }

        private static async Task PersistMetadata(IHlaMetadataRepository repository, IEnumerable<ISerialisableHlaMetadata> metadata, string hlaNomenclatureVersion)
        {
            await repository.RecreateHlaMetadataTable(metadata, hlaNomenclatureVersion);
        }
    }
}