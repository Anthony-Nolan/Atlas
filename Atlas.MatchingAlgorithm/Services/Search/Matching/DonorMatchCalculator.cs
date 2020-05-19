using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Common.Models.SearchResults;
using PGroup = System.Collections.Generic.List<string>;

namespace Atlas.MatchingAlgorithm.Services.Search.Matching
{
    public interface IDonorMatchCalculator
    {
        LocusMatchDetails CalculateMatchDetailsForDonorHla(AlleleLevelLocusMatchCriteria locusMatchCriteria, Tuple<IEnumerable<string>, IEnumerable<string>> pGroups);
    }

    public class DonorMatchCalculator : IDonorMatchCalculator
    {
        public LocusMatchDetails CalculateMatchDetailsForDonorHla(AlleleLevelLocusMatchCriteria locusMatchCriteria, Tuple<IEnumerable<string>, IEnumerable<string>> pGroups)
        {
            var hla1 =  pGroups.Item1?.ToList();
            var hla2 =  pGroups.Item2?.ToList();
            return CalculateMatchDetailsForDonorHla(locusMatchCriteria, new Tuple<PGroup, PGroup>(hla1, hla2));
        }

        private static LocusMatchDetails CalculateMatchDetailsForDonorHla(
            AlleleLevelLocusMatchCriteria locusMatchCriteria,
            Tuple<PGroup, PGroup> expandedHla
        )
        {
            if (expandedHla.Item1 == null ^ expandedHla.Item2 == null)
            {
                throw new ArgumentException("Locus cannot be partially typed. Either both positions should have data, or both should be null - check the validity of the matching data.");
            }
            
            return new LocusMatchDetails
            {
                MatchCount = CalculateMatchCount(locusMatchCriteria, expandedHla),
            };
        }

        private static int CalculateMatchCount(AlleleLevelLocusMatchCriteria locusMatchCriteria, Tuple<PGroup, PGroup> expandedHla)
        {
            var hla1 = expandedHla.Item1;
            var hla2 = expandedHla.Item2;

            // Assume a match until we know otherwise - untyped loci should count as a potential match
            var matchCount = 2;
            if (locusMatchCriteria != null && hla1 != null && hla2 != null)
            {
                // We have typed search and donor hla to compare
                matchCount = 0;

                var atLeastOneMatch = locusMatchCriteria.PGroupsToMatchInPositionOne.Any(pg => hla1.Union(hla2).Contains(pg)) ||
                                          locusMatchCriteria.PGroupsToMatchInPositionTwo.Any(pg => hla1.Union(hla2).Contains(pg));

                if (atLeastOneMatch)
                {
                    matchCount++;
                }

                var twoMatches = DirectMatch(locusMatchCriteria, expandedHla) || CrossMatch(locusMatchCriteria, expandedHla);
                if (twoMatches)
                {
                    matchCount++;
                }
            }

            return matchCount;
        }

        private static bool DirectMatch(AlleleLevelLocusMatchCriteria locusMatchCriteria, Tuple<PGroup, PGroup> expandedHla)
        {
            var hla1 = expandedHla.Item1;
            var hla2 = expandedHla.Item2;
            return locusMatchCriteria.PGroupsToMatchInPositionOne.Any(pg => hla1.Contains(pg)) &&
                   locusMatchCriteria.PGroupsToMatchInPositionTwo.Any(pg => hla2.Contains(pg));
        }

        private static bool CrossMatch(AlleleLevelLocusMatchCriteria locusMatchCriteria, Tuple<PGroup, PGroup> expandedHla)
        {
            var hla1 = expandedHla.Item1;
            var hla2 = expandedHla.Item2;
            return locusMatchCriteria.PGroupsToMatchInPositionOne.Any(pg => hla2.Contains(pg)) &&
                   locusMatchCriteria.PGroupsToMatchInPositionTwo.Any(pg => hla1.Contains(pg));
        }
    }
}