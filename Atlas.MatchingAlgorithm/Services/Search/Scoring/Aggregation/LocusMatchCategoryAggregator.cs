using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Models;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring.Aggregation
{
    internal static class LocusMatchCategoryAggregator
    {
        /// <summary>
        /// Consolidates locus scoring info at a locus into a single match category value.
        /// Does not take DPB1 specific behaviour (option of Permissive Mismatch) into account.
        /// To allow permissive mismatches, use the more specific <see cref="Dpb1MatchCategoryFromPositionScores"/>
        /// </summary>
        public static LocusMatchCategory LocusMatchCategoryFromPositionScores(LocusInfo<LocusPositionScoreDetails> positionScores)
        {
            var matchGradeRanking = new Dictionary<LocusMatchCategory, int>
            {
                {LocusMatchCategory.Unknown, 0},
                {LocusMatchCategory.Mismatch, 1},
                {LocusMatchCategory.PermissiveMismatch, 2},
                {LocusMatchCategory.Match, 3}
            };

            var categories = positionScores.Map(s => s.MatchGrade.ToLocusMatchCategory());


            if (categories.Position1 == LocusMatchCategory.Unknown ^ categories.Position2 == LocusMatchCategory.Unknown)
            {
                throw new ArgumentException("If one position has match grade 'unknown', the other should too");
            }

            return categories.ToEnumerable().OrderBy(x => matchGradeRanking[x]).First();
        }

        public static LocusMatchCategory Dpb1MatchCategoryFromPositionScores(
            LocusInfo<LocusPositionScoreDetails> positionScores,
            Dpb1TceGroupMatchType tceGroupMatchType
        )
        {
            var nonTceMatchCategory = LocusMatchCategoryFromPositionScores(positionScores);
            if (nonTceMatchCategory != LocusMatchCategory.Mismatch)
            {
                return nonTceMatchCategory;
            }

            switch (tceGroupMatchType)
            {
                case Dpb1TceGroupMatchType.Permissive:
                    return LocusMatchCategory.PermissiveMismatch;
                case Dpb1TceGroupMatchType.NonPermissiveHvG:
                case Dpb1TceGroupMatchType.NonPermissiveGvH:
                    return nonTceMatchCategory;
                case Dpb1TceGroupMatchType.Unknown:
                    return LocusMatchCategory.Unknown;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tceGroupMatchType), tceGroupMatchType, null);
            }
        }
    }
}