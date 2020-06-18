using System.Collections.Generic;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Matching.Services;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Common.Models.SearchResults;

namespace Atlas.MatchingAlgorithm.Services.Search.Matching
{
    public interface IDonorMatchCalculator
    {
        LocusMatchDetails CalculateMatchDetailsForDonorHla(AlleleLevelLocusMatchCriteria locusMatchCriteria,
            LocusInfo<IEnumerable<string>> pGroups);
    }

    public class DonorMatchCalculator : IDonorMatchCalculator
    {
        private readonly ILocusMatchCalculator locusMatchCalculator;

        public DonorMatchCalculator(ILocusMatchCalculator locusMatchCalculator)
        {
            this.locusMatchCalculator = locusMatchCalculator;
        }

        public LocusMatchDetails CalculateMatchDetailsForDonorHla(
            AlleleLevelLocusMatchCriteria locusMatchCriteria,
            LocusInfo<IEnumerable<string>> pGroups)
        {
            var expandedHla = new LocusInfo<IEnumerable<string>>(pGroups.Position1, pGroups.Position2);
            var expandedLocusMatchCriteria = 
                new LocusInfo<IEnumerable<string>>(locusMatchCriteria.PGroupsToMatchInPositionOne, locusMatchCriteria.PGroupsToMatchInPositionTwo);

            var matchCount = locusMatchCalculator.MatchCount(expandedLocusMatchCriteria, expandedHla);

            return new LocusMatchDetails
            {
                MatchCount = matchCount
            };
        }
    }
}