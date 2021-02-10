using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.InternalExceptions;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;
using System;
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
        private readonly IGGroupToPGroupMetadataRepository gGroupToPGroupMetadataRepository;
        private readonly IHlaNameToSmallGGroupLookupRepository hlaNameToSmallGGroupLookupRepository;
        private readonly ISmallGGroupToPGroupMetadataRepository smallGGroupToPGroupMetadataRepository;
        private readonly ILogger logger;

        public RecreateHlaMetadataService(
            IHlaMetadataGenerationOrchestrator hlaMetadataGenerationOrchestrator,
            IAlleleNamesMetadataRepository alleleNamesMetadataRepository,
            IHlaMatchingMetadataRepository hlaMatchingMetadataRepository,
            IHlaScoringMetadataRepository hlaScoringMetadataRepository,
            IDpb1TceGroupsMetadataRepository dpb1TceGroupsMetadataRepository,
            IAlleleGroupsMetadataRepository alleleGroupsMetadataRepository,
            IGGroupToPGroupMetadataRepository gGroupToPGroupMetadataRepository,
            IHlaNameToSmallGGroupLookupRepository hlaNameToSmallGGroupLookupRepository,
            ISmallGGroupToPGroupMetadataRepository smallGGroupToPGroupMetadataRepository,
            ILogger logger)
        {
            this.hlaMetadataGenerationOrchestrator = hlaMetadataGenerationOrchestrator;
            this.alleleNamesMetadataRepository = alleleNamesMetadataRepository;
            this.hlaMatchingMetadataRepository = hlaMatchingMetadataRepository;
            this.hlaScoringMetadataRepository = hlaScoringMetadataRepository;
            this.dpb1TceGroupsMetadataRepository = dpb1TceGroupsMetadataRepository;
            this.alleleGroupsMetadataRepository = alleleGroupsMetadataRepository;
            this.gGroupToPGroupMetadataRepository = gGroupToPGroupMetadataRepository;
            this.hlaNameToSmallGGroupLookupRepository = hlaNameToSmallGGroupLookupRepository;
            this.smallGGroupToPGroupMetadataRepository = smallGGroupToPGroupMetadataRepository;
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
                alleleNamesMetadataRepository.RecreateHlaMetadataTable(metadataCollection.AlleleNameMetadata, hlaNomenclatureVersion),
                hlaMatchingMetadataRepository.RecreateHlaMetadataTable(metadataCollection.HlaMatchingMetadata, hlaNomenclatureVersion),
                hlaScoringMetadataRepository.RecreateHlaMetadataTable(metadataCollection.HlaScoringMetadata, hlaNomenclatureVersion),
                dpb1TceGroupsMetadataRepository.RecreateHlaMetadataTable(metadataCollection.Dpb1TceGroupMetadata, hlaNomenclatureVersion),
                alleleGroupsMetadataRepository.RecreateHlaMetadataTable(metadataCollection.AlleleGroupMetadata, hlaNomenclatureVersion),
                gGroupToPGroupMetadataRepository.RecreateHlaMetadataTable(metadataCollection.GGroupToPGroupMetadata, hlaNomenclatureVersion),
                hlaNameToSmallGGroupLookupRepository.RecreateHlaMetadataTable(metadataCollection.SmallGGroupMetadata, hlaNomenclatureVersion),
                smallGGroupToPGroupMetadataRepository.RecreateHlaMetadataTable(metadataCollection.SmallGGroupToPGroupMetadata, hlaNomenclatureVersion)
            );
        }
    }
}