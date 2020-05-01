using Atlas.MatchingAlgorithm.MatchingDictionary.Exceptions;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.Lookups;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.Lookups.AlleleNameLookup;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.MatchingTypings;
using Atlas.MatchingAlgorithm.MatchingDictionary.Services.HlaDataConversion;
using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Utils.Core.ApplicationInsights;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Services
{
    /// <summary>
    /// Orchestrates the generation of the HLA Lookup Results dataset.
    /// The resulting data is not persisted.
    /// </summary>
    public interface IHlaLookupResultsService
    {
        HlaLookupResultCollections GetAllHlaLookupResults(string hlaDatabaseVersion);
    }

    public class HlaLookupResultsService : IHlaLookupResultsService
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

        public HlaLookupResultCollections GetAllHlaLookupResults(string hlaDatabaseVersion)
        {
            try
            {
                logger.SendTrace("MatchingDictionary: Processing Allele Names", LogLevel.Info);
                var alleleNameLookupResults = GetAlleleNamesAndTheirVariants(hlaDatabaseVersion);
                
                logger.SendTrace("MatchingDictionary: Processing Pre-calculated match hla", LogLevel.Info);
                var preCalculatedMatchedHla = GetPreCalculatedMatchedHla(hlaDatabaseVersion).ToList();
                
                logger.SendTrace("MatchingDictionary: Processing Matching lookup", LogLevel.Info);
                var matchingLookupResults = GetMatchingLookupResults(preCalculatedMatchedHla);

                logger.SendTrace("MatchingDictionary: Processing Scoring lookup", LogLevel.Info);
                var scoringLookupResults = GetScoringLookupResults(preCalculatedMatchedHla);

                logger.SendTrace("MatchingDictionary: Processing TCE group lookup", LogLevel.Info);
                var dpb1TceGroupLookupResults = GetDpb1TceGroupLookupResults(hlaDatabaseVersion);

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
                throw new MatchingDictionaryHttpException("Could not get all HLA lookup results.", ex);
            }
        }

        private IEnumerable<IMatchedHla> GetPreCalculatedMatchedHla(string hlaDatabaseVersion)
        {
            return matchPreCalculationService.GetMatchedHla(hlaDatabaseVersion);
        }

        private IEnumerable<IAlleleNameLookupResult> GetAlleleNamesAndTheirVariants(string hlaDatabaseVersion)
        {
            return alleleNamesService.GetAlleleNamesAndTheirVariants(hlaDatabaseVersion);
        }

        private IEnumerable<IHlaLookupResult> GetMatchingLookupResults(IEnumerable<IMatchedHla> matchedHla)
        {
            return hlaMatchingDataConverter.ConvertToHlaLookupResults(matchedHla);
        }

        private IEnumerable<IHlaLookupResult> GetScoringLookupResults(IEnumerable<IMatchedHla> matchedHla)
        {
            return hlaScoringDataConverter.ConvertToHlaLookupResults(matchedHla);
        }

        private IEnumerable<IHlaLookupResult> GetDpb1TceGroupLookupResults(string hlaDatabaseVersion)
        {
            return dpb1TceGroupsService.GetDpb1TceGroupLookupResults(hlaDatabaseVersion);
        }
    }
}