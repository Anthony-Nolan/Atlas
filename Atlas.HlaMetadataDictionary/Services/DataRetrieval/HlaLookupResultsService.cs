using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.Exceptions;
using Atlas.HlaMetadataDictionary.Models.Lookups;
using Atlas.HlaMetadataDictionary.Models.Lookups.AlleleNameLookup;
using Atlas.HlaMetadataDictionary.Models.MatchingTypings;
using Atlas.HlaMetadataDictionary.Services.DataGeneration;
using Atlas.HlaMetadataDictionary.Services.DataGeneration.HlaMatchPreCalculation;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval.HlaDataConversion;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval
{
    /// <summary>
    /// Orchestrates the generation of the HLA Lookup Results dataset.
    /// The resulting data is not persisted.
    /// </summary>
    internal interface IHlaLookupResultsService
    {
        HlaLookupResultCollections GetAllHlaLookupResults(string hlaNomenclatureVersion);
    }

    internal class HlaLookupResultsService : IHlaLookupResultsService
    {
        private readonly IHlaMatchPreCalculationService matchPreCalculationService;
        private readonly IAlleleNamesService alleleNamesService;
        private readonly IHlaMatchingDataConverter hlaMatchingDataConverter;
        private readonly IHlaScoringDataConverter hlaScoringDataConverter;
        private readonly IDpb1TceGroupsService dpb1TceGroupsService;
        private readonly ILogger logger;

        public HlaLookupResultsService(
            IHlaMatchPreCalculationService matchPreCalculationService,
            IAlleleNamesService alleleNamesService,
            IHlaMatchingDataConverter hlaMatchingDataConverter,
            IHlaScoringDataConverter hlaScoringDataConverter,
            IDpb1TceGroupsService dpb1TceGroupsService,
            ILogger logger)
        {
            this.matchPreCalculationService = matchPreCalculationService;
            this.alleleNamesService = alleleNamesService;
            this.hlaMatchingDataConverter = hlaMatchingDataConverter;
            this.hlaScoringDataConverter = hlaScoringDataConverter;
            this.dpb1TceGroupsService = dpb1TceGroupsService;
            this.logger = logger;
        }

        public HlaLookupResultCollections GetAllHlaLookupResults(string hlaNomenclatureVersion)
        {
            try
            {
                logger.SendTrace("HlaMetadataDictionary: Processing Allele Names", LogLevel.Info);
                var alleleNameLookupResults = GetAlleleNamesAndTheirVariants(hlaNomenclatureVersion);
                
                logger.SendTrace("HlaMetadataDictionary: Processing Pre-calculated match hla", LogLevel.Info);
                var preCalculatedMatchedHla = GetPreCalculatedMatchedHla(hlaNomenclatureVersion).ToList();
                
                logger.SendTrace("HlaMetadataDictionary: Processing Matching lookup", LogLevel.Info);
                var matchingLookupResults = GetMatchingLookupResults(preCalculatedMatchedHla);

                logger.SendTrace("HlaMetadataDictionary: Processing Scoring lookup", LogLevel.Info);
                var scoringLookupResults = GetScoringLookupResults(preCalculatedMatchedHla);

                logger.SendTrace("HlaMetadataDictionary: Processing TCE group lookup", LogLevel.Info);
                var dpb1TceGroupLookupResults = GetDpb1TceGroupLookupResults(hlaNomenclatureVersion);

                return new HlaLookupResultCollections
                {
                    AlleleNameLookupResults = alleleNameLookupResults,
                    HlaMatchingLookupResults = matchingLookupResults,
                    HlaScoringLookupResults = scoringLookupResults,
                    Dpb1TceGroupLookupResults = dpb1TceGroupLookupResults
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

        private IEnumerable<IAlleleNameLookupResult> GetAlleleNamesAndTheirVariants(string hlaNomenclatureVersion)
        {
            return alleleNamesService.GetAlleleNamesAndTheirVariants(hlaNomenclatureVersion);
        }

        private IEnumerable<ISerialisableHlaMetadata> GetMatchingLookupResults(IEnumerable<IMatchedHla> matchedHla)
        {
            return hlaMatchingDataConverter.ConvertToHlaLookupResults(matchedHla);
        }

        private IEnumerable<ISerialisableHlaMetadata> GetScoringLookupResults(IEnumerable<IMatchedHla> matchedHla)
        {
            return hlaScoringDataConverter.ConvertToHlaLookupResults(matchedHla);
        }

        private IEnumerable<ISerialisableHlaMetadata> GetDpb1TceGroupLookupResults(string hlaNomenclatureVersion)
        {
            return dpb1TceGroupsService.GetDpb1TceGroupLookupResults(hlaNomenclatureVersion);
        }
    }
}