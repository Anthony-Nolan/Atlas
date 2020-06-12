using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.InternalExceptions;
using Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;
using Atlas.HlaMetadataDictionary.Services.DataGeneration;
using Atlas.HlaMetadataDictionary.Services.DataGeneration.HlaMatchPreCalculation;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval.MatchedHlaConversion;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval
{
    /// <summary>
    /// Orchestrates the generation of the HLA Metadata dataset.
    /// The resulting data has not yet been persisted.
    /// </summary>
    internal interface IHlaMetadataService
    {
        HlaMetadataCollectionForSerialisation GetAllHlaMetadata(string hlaNomenclatureVersion);
    }

    internal class HlaMetadataService : IHlaMetadataService
    {
        private readonly IHlaMatchPreCalculationService matchPreCalculationService;
        private readonly IAlleleNamesService alleleNamesService;
        private readonly IHlaToMatchingMetaDataConverter hlaToMatchingMetaDataConverter;
        private readonly IHlaToScoringMetaDataConverter hlaToScoringMetaDataConverter;
        private readonly IDpb1TceGroupsService dpb1TceGroupsService;
        private readonly ILogger logger;

        public HlaMetadataService(
            IHlaMatchPreCalculationService matchPreCalculationService,
            IAlleleNamesService alleleNamesService,
            IHlaToMatchingMetaDataConverter hlaToMatchingMetaDataConverter,
            IHlaToScoringMetaDataConverter hlaToScoringMetaDataConverter,
            IDpb1TceGroupsService dpb1TceGroupsService,
            ILogger logger)
        {
            this.matchPreCalculationService = matchPreCalculationService;
            this.alleleNamesService = alleleNamesService;
            this.hlaToMatchingMetaDataConverter = hlaToMatchingMetaDataConverter;
            this.hlaToScoringMetaDataConverter = hlaToScoringMetaDataConverter;
            this.dpb1TceGroupsService = dpb1TceGroupsService;
            this.logger = logger;
        }

        public HlaMetadataCollectionForSerialisation GetAllHlaMetadata(string hlaNomenclatureVersion)
        {
            try
            {
                logger.SendTrace("HlaMetadataDictionary: Processing Allele Names", LogLevel.Info);
                var alleleNameMetadata = GetAlleleNamesAndTheirVariants(hlaNomenclatureVersion);
                
                logger.SendTrace("HlaMetadataDictionary: Processing Pre-calculated match hla", LogLevel.Info);
                var preCalculatedMatchedHla = GetPreCalculatedMatchedHla(hlaNomenclatureVersion).ToList();
                
                logger.SendTrace("HlaMetadataDictionary: Processing Matching lookup", LogLevel.Info);
                var matchingMetadata = GetMatchingMetadata(preCalculatedMatchedHla);

                logger.SendTrace("HlaMetadataDictionary: Processing Scoring lookup", LogLevel.Info);
                var scoringMetadata = GetScoringMetadata(preCalculatedMatchedHla);

                logger.SendTrace("HlaMetadataDictionary: Processing TCE group lookup", LogLevel.Info);
                var dpb1TceGroupMetadata = GetDpb1TceGroupMetadata(hlaNomenclatureVersion);

                return new HlaMetadataCollectionForSerialisation()
                {
                    AlleleNameMetadata = alleleNameMetadata,
                    HlaMatchingMetadata = matchingMetadata,
                    HlaScoringMetadata = scoringMetadata,
                    Dpb1TceGroupMetadata = dpb1TceGroupMetadata
                };
            }
            catch (Exception ex)
            {
                throw new HlaMetadataDictionaryHttpException("Could not get all HLA lookup results.", ex);
            }
        }

        private IEnumerable<IMatchedHla> GetPreCalculatedMatchedHla(string hlaNomenclatureVersion)
        {
            return matchPreCalculationService.GetMatchedHla(hlaNomenclatureVersion);
        }

        private IEnumerable<IAlleleNameMetadata> GetAlleleNamesAndTheirVariants(string hlaNomenclatureVersion)
        {
            return alleleNamesService.GetAlleleNamesAndTheirVariants(hlaNomenclatureVersion);
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
    }
}