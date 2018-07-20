using System;
using System.Collections.Generic;
using System.Linq;
using Castle.Core.Internal;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.SearchResults;

namespace Nova.SearchAlgorithm.Services.Matching
{
    public interface IDonorMatchCalculator
    {
        LocusMatchDetails CalculateMatchDetailsForDonorHla(AlleleLevelLocusMatchCriteria locusMatchCriteria, Tuple<ExpandedHla, ExpandedHla> expandedHla);
        LocusMatchDetails CalculateMatchDetailsForDonorHla(AlleleLevelLocusMatchCriteria locusMatchCriteria, Tuple<IEnumerable<string>, IEnumerable<string>> pGroups);
    }

    public class DonorMatchCalculator : IDonorMatchCalculator
    {
        public LocusMatchDetails CalculateMatchDetailsForDonorHla(AlleleLevelLocusMatchCriteria locusMatchCriteria, Tuple<IEnumerable<string>, IEnumerable<string>> pGroups)
        {
            var hla1 =  pGroups.Item1.IsNullOrEmpty() ? null : new ExpandedHla{PGroups = pGroups.Item1 };
            var hla2 =  pGroups.Item2.IsNullOrEmpty() ? null : new ExpandedHla{PGroups = pGroups.Item2 };
            return CalculateMatchDetailsForDonorHla(locusMatchCriteria, new Tuple<ExpandedHla, ExpandedHla>(hla1, hla2));
        }

        public LocusMatchDetails CalculateMatchDetailsForDonorHla(
            AlleleLevelLocusMatchCriteria locusMatchCriteria,
            Tuple<ExpandedHla, ExpandedHla> expandedHla
        )
        {
            if (expandedHla.Item1 == null ^ expandedHla.Item2 == null)
            {
                throw new ArgumentException("Locus cannot be partially typed. Either both positions should have data, or both should be null - check the validity of the matching data.");
            }
            
            return new LocusMatchDetails
            {
                MatchCount = CalculateMatchCount(locusMatchCriteria, expandedHla),
                IsLocusTyped = IsLocusTyped(expandedHla),
            };
        }

        private static bool IsLocusTyped(Tuple<ExpandedHla, ExpandedHla> expandedHla)
        {
            return expandedHla.Item1 != null && expandedHla.Item2 != null;
        }

        private static int CalculateMatchCount(AlleleLevelLocusMatchCriteria locusMatchCriteria, Tuple<ExpandedHla, ExpandedHla> expandedHla)
        {
            var hla1 = expandedHla.Item1;
            var hla2 = expandedHla.Item2;

            // Assume a match until we know otherwise - untyped loci should count as a potential match
            var matchCount = 2;
            if (locusMatchCriteria != null && hla1 != null && hla2 != null)
            {
                // We have typed search and donor hla to compare
                matchCount = 0;

                var atLeastOneMatch = locusMatchCriteria.PGroupsToMatchInPositionOne.Any(pg => hla1.PGroups.Union(hla2.PGroups).Contains(pg)) ||
                                      locusMatchCriteria.PGroupsToMatchInPositionTwo.Any(pg => hla1.PGroups.Union(hla2.PGroups).Contains(pg));

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

        private static bool DirectMatch(AlleleLevelLocusMatchCriteria locusMatchCriteria, Tuple<ExpandedHla, ExpandedHla> expandedHla)
        {
            var hla1 = expandedHla.Item1;
            var hla2 = expandedHla.Item2;
            return locusMatchCriteria.PGroupsToMatchInPositionOne.Any(pg => hla1.PGroups.Contains(pg)) &&
                   locusMatchCriteria.PGroupsToMatchInPositionTwo.Any(pg => hla2.PGroups.Contains(pg));
        }

        private static bool CrossMatch(AlleleLevelLocusMatchCriteria locusMatchCriteria, Tuple<ExpandedHla, ExpandedHla> expandedHla)
        {
            var hla1 = expandedHla.Item1;
            var hla2 = expandedHla.Item2;
            return locusMatchCriteria.PGroupsToMatchInPositionOne.Any(pg => hla2.PGroups.Contains(pg)) &&
                   locusMatchCriteria.PGroupsToMatchInPositionTwo.Any(pg => hla1.PGroups.Contains(pg));
        }
    }
}