using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.InternalExceptions;
using Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;
using Atlas.HlaMetadataDictionary.Services.DataGeneration.HlaMatchPreCalculation;
using Atlas.HlaMetadataDictionary.Services.DataGeneration.MatchedHlaConversion;

namespace Atlas.HlaMetadataDictionary.Services.DataGeneration
{
    /// <summary>
    /// Orchestrates the generation of the HLA Metadata dataset.
    /// The resulting data has not yet been persisted.
    /// </summary>
    internal interface IHlaMetadataGenerationOrchestrator
    {
        HlaMetadataCollection GenerateAllHlaMetadata(string hlaNomenclatureVersion);
    }

    internal class HlaMetadataGenerationOrchestrator : IHlaMetadataGenerationOrchestrator
    {
        private readonly IHlaMatchPreCalculationService matchPreCalculationService;
        private readonly IAlleleNamesService alleleNamesService;
        private readonly IHlaToMatchingMetaDataConverter hlaToMatchingMetaDataConverter;
        private readonly IHlaToScoringMetaDataConverter hlaToScoringMetaDataConverter;
        private readonly IDpb1TceGroupsService dpb1TceGroupsService;
        private readonly IAlleleGroupsService alleleGroupsService;
        private readonly IGGroupToPGroupService gGroupToPGroupService;
        private readonly ISmallGGroupsService smallGGroupsService;
        private readonly ILogger logger;

        public HlaMetadataGenerationOrchestrator(
            IHlaMatchPreCalculationService matchPreCalculationService,
            IAlleleNamesService alleleNamesService,
            IHlaToMatchingMetaDataConverter hlaToMatchingMetaDataConverter,
            IHlaToScoringMetaDataConverter hlaToScoringMetaDataConverter,
            IDpb1TceGroupsService dpb1TceGroupsService,
            IAlleleGroupsService alleleGroupsService,
            IGGroupToPGroupService gGroupToPGroupService,
            ISmallGGroupsService smallGGroupsService,
            ILogger logger)
        {
            this.matchPreCalculationService = matchPreCalculationService;
            this.alleleNamesService = alleleNamesService;
            this.hlaToMatchingMetaDataConverter = hlaToMatchingMetaDataConverter;
            this.hlaToScoringMetaDataConverter = hlaToScoringMetaDataConverter;
            this.dpb1TceGroupsService = dpb1TceGroupsService;
            this.alleleGroupsService = alleleGroupsService;
            this.gGroupToPGroupService = gGroupToPGroupService;
            this.smallGGroupsService = smallGGroupsService;
            this.logger = logger;
        }

        public HlaMetadataCollection GenerateAllHlaMetadata(string hlaNomenclatureVersion)
        {
            try
            {
                logger.SendTrace("HlaMetadataDictionary: Processing Allele Names");
                var alleleNameMetadata = GetAlleleNamesAndTheirVariants(hlaNomenclatureVersion).ToList();
                
                logger.SendTrace("HlaMetadataDictionary: Processing Pre-calculated match hla");
                var preCalculatedMatchedHla = GetPreCalculatedMatchedHla(hlaNomenclatureVersion).ToList();

                logger.SendTrace("HlaMetadataDictionary: Processing Matching metadata");
                var matchingMetadata = GetMatchingMetadata(preCalculatedMatchedHla).ToList();

                logger.SendTrace("HlaMetadataDictionary: Processing Scoring metadata");
                var scoringMetadata = GetScoringMetadata(preCalculatedMatchedHla).ToList();

                logger.SendTrace("HlaMetadataDictionary: Processing TCE groups");
                var dpb1TceGroupMetadata = GetDpb1TceGroupMetadata(hlaNomenclatureVersion).ToList();

                logger.SendTrace("HlaMetadataDictionary: Processing Allele Groups metadata");
                var alleleGroupsMetadata = GetAlleleGroupsMetadata(hlaNomenclatureVersion).ToList();

                logger.SendTrace("HlaMetadataDictionary: GGroup to PGroup");
                var gGroupToPGroupMetadata = GetGGroupToPGroupMetadata(hlaNomenclatureVersion).ToList();

                logger.SendTrace("HlaMetadataDictionary: Building small G groups");
                var smallGGroupsMetadata = GetSmallGGroupsMetadata(hlaNomenclatureVersion).ToList();

                return new HlaMetadataCollection
                {
                    AlleleNameMetadata = alleleNameMetadata,
                    HlaMatchingMetadata = matchingMetadata,
                    HlaScoringMetadata = scoringMetadata,
                    Dpb1TceGroupMetadata = dpb1TceGroupMetadata,
                    AlleleGroupMetadata = alleleGroupsMetadata,
                    GGroupToPGroupMetadata = gGroupToPGroupMetadata,
                    SmallGGroupMetadata = smallGGroupsMetadata
                };
            }
            catch (Exception ex)
            {
                throw new HlaMetadataDictionaryHttpException("Could not get all HLA lookup results.", ex);
            }
        }

        private IEnumerable<IAlleleNameMetadata> GetAlleleNamesAndTheirVariants(string hlaNomenclatureVersion)
        {
            return alleleNamesService.GetAlleleNamesAndTheirVariants(hlaNomenclatureVersion);
        }

        private IEnumerable<IMatchedHla> GetPreCalculatedMatchedHla(string hlaNomenclatureVersion)
        {
            return matchPreCalculationService.GetMatchedHla(hlaNomenclatureVersion);
        }

        private IEnumerable<ISerialisableHlaMetadata> GetMatchingMetadata(IEnumerable<IMatchedHla> matchedHla)
        {
            return hlaToMatchingMetaDataConverter.ConvertToHlaMetadata(matchedHla);
        }

        private IEnumerable<ISerialisableHlaMetadata> GetScoringMetadata(IEnumerable<IMatchedHla> matchedHla)
        {
            return hlaToScoringMetaDataConverter.ConvertToHlaMetadata(matchedHla);
        }

        private IEnumerable<ISerialisableHlaMetadata> GetDpb1TceGroupMetadata(string hlaNomenclatureVersion)
        {
            return dpb1TceGroupsService.GetDpb1TceGroupMetadata(hlaNomenclatureVersion);
        }

        private IEnumerable<ISerialisableHlaMetadata> GetAlleleGroupsMetadata(string hlaNomenclatureVersion)
        {
            return alleleGroupsService.GetAlleleGroupsMetadata(hlaNomenclatureVersion);
        }

        private IEnumerable<ISerialisableHlaMetadata> GetGGroupToPGroupMetadata(string hlaNomenclatureVersion)
        {
            return gGroupToPGroupService.GetGGroupToPGroupMetadata(hlaNomenclatureVersion);
        }

        private IEnumerable<ISmallGGroupsMetadata> GetSmallGGroupsMetadata(string hlaNomenclatureVersion)
        {
            return smallGGroupsService.GetSmallGGroupMetadata(hlaNomenclatureVersion);
        }
    }
}