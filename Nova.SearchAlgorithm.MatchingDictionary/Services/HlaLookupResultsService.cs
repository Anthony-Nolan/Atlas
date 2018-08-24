using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.AlleleNameLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using Nova.SearchAlgorithm.MatchingDictionary.Services.HlaDataConversion;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services
{
    /// <summary>
    /// Orchestrates the generation of the HLA Lookup Results dataset.
    /// The resulting data is not persisted.
    /// </summary>
    public interface IHlaLookupResultsService
    {
        HlaLookupResultCollections GetAllHlaLookupResults();
    }

    public class HlaLookupResultsService : IHlaLookupResultsService
    {
        private readonly IHlaMatchPreCalculationService matchPreCalculationService;
        private readonly IAlleleNamesService alleleNamesService;
        private readonly IHlaMatchingDataConverter hlaMatchingDataConverter;
        private readonly IHlaScoringDataConverter hlaScoringDataConverter;
        private readonly IDpb1TceGroupsService dpb1TceGroupsService;

        public HlaLookupResultsService(
            IHlaMatchPreCalculationService matchPreCalculationService,
            IAlleleNamesService alleleNamesService,
            IHlaMatchingDataConverter hlaMatchingDataConverter,
            IHlaScoringDataConverter hlaScoringDataConverter,
            IDpb1TceGroupsService dpb1TceGroupsService)
        {
            this.matchPreCalculationService = matchPreCalculationService;
            this.alleleNamesService = alleleNamesService;
            this.hlaMatchingDataConverter = hlaMatchingDataConverter;
            this.hlaScoringDataConverter = hlaScoringDataConverter;
            this.dpb1TceGroupsService = dpb1TceGroupsService;
        }

        public HlaLookupResultCollections GetAllHlaLookupResults()
        {
            try
            {
                var alleleNameLookupResults = GetAlleleNamesAndTheirVariants();

                var precalculatedMatchedHla = GetPrecalculateMatchedHla().ToList();
                var matchingLookupResults = GetMatchingLookupResults(precalculatedMatchedHla);
                var scoringLookupResults = GetScoringLookupResults(precalculatedMatchedHla);

                var dpb1TceGroupLookupResults = GetDpb1TceGroupLookupResults();

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

        private IEnumerable<IMatchedHla> GetPrecalculateMatchedHla()
        {
            return matchPreCalculationService.GetMatchedHla();
        }

        private IEnumerable<IAlleleNameLookupResult> GetAlleleNamesAndTheirVariants()
        {
            return alleleNamesService.GetAlleleNamesAndTheirVariants();
        }

        private IEnumerable<IHlaLookupResult> GetMatchingLookupResults(IEnumerable<IMatchedHla> matchedHla)
        {
            return hlaMatchingDataConverter.ConvertToHlaLookupResults(matchedHla);
        }

        private IEnumerable<IHlaLookupResult> GetScoringLookupResults(IEnumerable<IMatchedHla> matchedHla)
        {
            return hlaScoringDataConverter.ConvertToHlaLookupResults(matchedHla);
        }

        private IEnumerable<IHlaLookupResult> GetDpb1TceGroupLookupResults()
        {
            return dpb1TceGroupsService.GetDpb1TceGroupLookupResults();
        }
    }
}