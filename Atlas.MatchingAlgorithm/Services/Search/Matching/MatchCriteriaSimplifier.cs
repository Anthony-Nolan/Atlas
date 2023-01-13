using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchingAlgorithm.Common.Models;

namespace Atlas.MatchingAlgorithm.Services.Search.Matching
{
    /// <summary>
    /// This class is predicated on the fact that a search with at least one *required* locus requiring 0 mismatches is *significantly* faster to run in Atlas.
    /// We aim to split any mismatch searches into multiple searches that fit this criteria as closely as possible.
    /// We can avoid running such "difficult" searches altogether with overall match counts less than 3, and for higher match counts we still need to run
    /// "difficult" searches, but can still split to make them less difficult.  
    /// </summary>
    internal static class MatchCriteriaSimplifier
    {
        public static List<AlleleLevelMatchCriteria> SplitSearch(AlleleLevelMatchCriteria criteria)
        {
            var originalSearch = new List<AlleleLevelMatchCriteria> { criteria };

            // If any required locus does not allow mismatches, the search is already easy enough to run - no splitting is required, regardless of other criteria
            if (criteria.LocusCriteria.A.MismatchCount == 0
                || criteria.LocusCriteria.B.MismatchCount == 0
                || criteria.LocusCriteria.Drb1.MismatchCount == 0)
            {
                return originalSearch;
            }

            switch (criteria.DonorMismatchCount)
            {
                case 0:
                default:
                    return originalSearch;
                case 1:
                    return SplitSingleMismatchSearch(criteria);
                case 2:
                    return SplitDoubleMismatchSearch(criteria);
                case 3:
                    return SplitTripleMismatchSearch(criteria);
            }
        }

        private static List<AlleleLevelMatchCriteria> SplitSingleMismatchSearch(AlleleLevelMatchCriteria criteria)
        {
            return new List<AlleleLevelMatchCriteria>
            {
                // Of the two split searches, one can allow mismatches at multiple loci, and one at only one locus.
                // The search with mismatches at multiple loci is considered more difficult, so we've chosen DRB1 as the locus to enforce a match at. 
                // As DRB1 is a slightly newer locus, the theory is that typing will generally be a little higher resolution at this locus, and so fewer matches will be found.
                // TODO: #709: Work out if splitting into 3 searches is actually quicker than two. 
                criteria.CopyWithNoMismatchesAtLocus(Locus.Drb1),
                criteria.CopyWithNoMismatchesExceptAtLocus(Locus.Drb1)
            };
        }

        private static List<AlleleLevelMatchCriteria> SplitDoubleMismatchSearch(AlleleLevelMatchCriteria criteria)
        {
            var aMismatches = criteria.LocusCriteria.A.MismatchCount;
            var bMismatches = criteria.LocusCriteria.B.MismatchCount;
            var drb1Mismatches = criteria.LocusCriteria.Drb1.MismatchCount;

            if (aMismatches == 1 && bMismatches == 1 && drb1Mismatches == 1)
            {
                return new List<AlleleLevelMatchCriteria>
                {
                    criteria.CopyWithNoMismatchesAtLocus(Locus.A),
                    criteria.CopyWithNoMismatchesAtLocus(Locus.B),
                    criteria.CopyWithNoMismatchesAtLocus(Locus.Drb1),
                };
            }

            var searchWithOneMismatchPerLocus = criteria.MapLocusCriteria((l, c) =>
                // If optional locus does not allow mismatches, we shouldn't start allowing them here - otherwise we set all loci to allow 1 mismatch,
                // to cover the case where one mismatch is at our "zeroed" locus, and one is not. 
                c?.WithXMismatches(Math.Min(c.MismatchCount, 1))
            );

            var splitSearchWithOneMismatchPerLocus = SplitSearch(searchWithOneMismatchPerLocus);

            if (aMismatches == 2 && bMismatches == 2 && drb1Mismatches == 2)
            {
                return new List<AlleleLevelMatchCriteria>
                {
                    criteria.CopyWithNoMismatchesAtLocus(Locus.Drb1),
                    criteria.CopyWithNoMismatchesExceptAtLocus(Locus.Drb1)
                }.Concat(splitSearchWithOneMismatchPerLocus).ToList();
            }

            return new List<AlleleLevelMatchCriteria>
            {
                criteria.MapLocusCriteria((l, c) => c?.MismatchCount == 1 && l.IsRequired() ? c.WithNoMismatches() : c)
            }.Concat(splitSearchWithOneMismatchPerLocus).ToList();
        }

        private static List<AlleleLevelMatchCriteria> SplitTripleMismatchSearch(AlleleLevelMatchCriteria criteria)
        {
            return new List<AlleleLevelMatchCriteria>
            {
                criteria.CopyWithNoMismatchesAtLocus(Locus.A),
                criteria.CopyWithNoMismatchesAtLocus(Locus.B),
                criteria.CopyWithNoMismatchesAtLocus(Locus.Drb1),
                criteria.MapLocusCriteria((l, c) => l.IsRequired() ? c?.WithXMismatches(Math.Min(c.MismatchCount, 1)) : c)
            };
        }

        private static AlleleLevelLocusMatchCriteria WithNoMismatches(this AlleleLevelLocusMatchCriteria criteria) => criteria?.WithXMismatches(0);

        private static AlleleLevelLocusMatchCriteria WithOneMismatch(this AlleleLevelLocusMatchCriteria criteria) => criteria?.WithXMismatches(1);

        private static AlleleLevelLocusMatchCriteria WithXMismatches(this AlleleLevelLocusMatchCriteria criteria, int x)
        {
            if (criteria == null)
            {
                return null;
            }

            return new AlleleLevelLocusMatchCriteria
            {
                MismatchCount = x,
                PGroupsToMatchInPositionOne = criteria.PGroupsToMatchInPositionOne,
                PGroupsToMatchInPositionTwo = criteria.PGroupsToMatchInPositionTwo
            };
        }

        private static AlleleLevelMatchCriteria CopyWithNoMismatchesAtLocus(this AlleleLevelMatchCriteria criteria, Locus locus)
        {
            return criteria.MapLocusCriteria((l, c) => l == locus ? c?.WithNoMismatches() : c);
        }

        private static AlleleLevelMatchCriteria CopyWithNoMismatchesExceptAtLocus(this AlleleLevelMatchCriteria criteria, Locus locus)
        {
            return criteria.MapLocusCriteria((l, c) => l != locus ? c?.WithNoMismatches() : c);
        }

        private static AlleleLevelMatchCriteria MapLocusCriteria(
            this AlleleLevelMatchCriteria criteria,
            Func<Locus, AlleleLevelLocusMatchCriteria, AlleleLevelLocusMatchCriteria> map)
        {
            return new AlleleLevelMatchCriteria
            {
                LocusCriteria = criteria.LocusCriteria.Map(map),
                SearchType = criteria.SearchType,
                DonorMismatchCount = criteria.DonorMismatchCount,
                ShouldIncludeBetterMatches = criteria.ShouldIncludeBetterMatches
            };
        }

        private static bool IsRequired(this Locus l) => l == Locus.A || l == Locus.B || l == Locus.Drb1;
    }
}