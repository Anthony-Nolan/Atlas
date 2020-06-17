using System;
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
        private readonly IAlleleGroupsMatchingCount alleleGroupsMatchingCount;

        public DonorMatchCalculator(IAlleleGroupsMatchingCount alleleGroupsMatchingCount)
        {
            this.alleleGroupsMatchingCount = alleleGroupsMatchingCount;
        }

        public LocusMatchDetails CalculateMatchDetailsForDonorHla(
            AlleleLevelLocusMatchCriteria locusMatchCriteria,
            LocusInfo<IEnumerable<string>> pGroups)
        {
            var expandedHla = new LocusInfo<IEnumerable<string>>(pGroups.Position1, pGroups.Position2);
            var expandedLocusMatchCriteria = 
                new LocusInfo<IEnumerable<string>>(locusMatchCriteria.PGroupsToMatchInPositionOne, locusMatchCriteria.PGroupsToMatchInPositionTwo);

            if (expandedHla.Position1 == null ^ expandedHla.Position2 == null)
            {
                throw new ArgumentException(
                    "Locus cannot be partially typed. Either both positions should have data, or both should be null - check the validity of the matching data.");
            }

            var matchCount = alleleGroupsMatchingCount.MatchCount(expandedLocusMatchCriteria, expandedHla);

            return new LocusMatchDetails
            {
                MatchCount = matchCount
            };
        }
    }
}