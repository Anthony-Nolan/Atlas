using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Services.Matching
{
    public interface IDonorMatchCalculator
    {
        LocusMatchDetails CalculateMatchDetailsForDonorHla(AlleleLevelLocusMatchCriteria locusMatchCriteria, Tuple<ExpandedHla, ExpandedHla> expandedHla);
    }

    public class DonorMatchCalculator : IDonorMatchCalculator
    {
        public LocusMatchDetails CalculateMatchDetailsForDonorHla(AlleleLevelLocusMatchCriteria locusMatchCriteria,
            Tuple<ExpandedHla, ExpandedHla> expandedHla)
        {
            var hla1 = expandedHla.Item1;
            var hla2 = expandedHla.Item2;
            var matchDetails = new LocusMatchDetails
            {
                // Assume a match until we know otherwise - untyped loci should count as a potential match
                MatchCount = 2,
                IsLocusTyped = hla1 != null || hla2 != null,
            };

            if (locusMatchCriteria != null && hla1 != null && hla2 != null)
            {
                // We have typed search and donor hla to compare
                matchDetails.MatchCount = 0;

                var atLeastOneMatch = locusMatchCriteria.HlaNamesToMatchInPositionOne.Any(pg => hla1.PGroups.Union(hla2.PGroups).Contains(pg)) || 
                                      locusMatchCriteria.HlaNamesToMatchInPositionTwo.Any(pg => hla1.PGroups.Union(hla2.PGroups).Contains(pg));

                if (atLeastOneMatch)
                {
                    matchDetails.MatchCount++;
                }

                var twoMatches = DirectMatch(locusMatchCriteria, expandedHla) || CrossMatch(locusMatchCriteria, expandedHla);
                if (twoMatches)
                {
                    matchDetails.MatchCount++;
                }
            }

            return matchDetails;
        }

        private static bool DirectMatch(AlleleLevelLocusMatchCriteria locusMatchCriteria, Tuple<ExpandedHla, ExpandedHla> expandedHla)
        {
            var hla1 = expandedHla.Item1;
            var hla2 = expandedHla.Item2;
            return locusMatchCriteria.HlaNamesToMatchInPositionOne.Any(pg => hla1.PGroups.Contains(pg)) &&
                   locusMatchCriteria.HlaNamesToMatchInPositionTwo.Any(pg => hla2.PGroups.Contains(pg));
        }

        private static bool CrossMatch(AlleleLevelLocusMatchCriteria locusMatchCriteria, Tuple<ExpandedHla, ExpandedHla> expandedHla)
        {
            var hla1 = expandedHla.Item1;
            var hla2 = expandedHla.Item2;
            return locusMatchCriteria.HlaNamesToMatchInPositionOne.Any(pg => hla2.PGroups.Contains(pg)) && 
                   locusMatchCriteria.HlaNamesToMatchInPositionTwo.Any(pg => hla1.PGroups.Contains(pg));
        }
    }
}