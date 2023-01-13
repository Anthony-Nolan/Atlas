using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
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
            var matchCategoryRanking = new Dictionary<LocusMatchCategory, int>
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

            return categories.ToEnumerable().OrderBy(x => matchCategoryRanking[x]).First();
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

            return tceGroupMatchType switch
            {
                Dpb1TceGroupMatchType.Permissive => LocusMatchCategory.PermissiveMismatch,
                Dpb1TceGroupMatchType.NonPermissiveHvG => nonTceMatchCategory,
                Dpb1TceGroupMatchType.NonPermissiveGvH => nonTceMatchCategory,
                Dpb1TceGroupMatchType.Unknown => LocusMatchCategory.Unknown,
                _ => throw new ArgumentOutOfRangeException(nameof(tceGroupMatchType), tceGroupMatchType, null),
            };
        }

        public static MismatchDirection? GetMismatchDirection(Dpb1TceGroupMatchType dpb1TceGroupMatchType)
        {
            return dpb1TceGroupMatchType switch
            {
                Dpb1TceGroupMatchType.NonPermissiveGvH => MismatchDirection.NonPermissiveGvH,
                Dpb1TceGroupMatchType.NonPermissiveHvG => MismatchDirection.NonPermissiveHvG,
                Dpb1TceGroupMatchType.Unknown => null,
                _ => MismatchDirection.NotApplicable
            };
        }
    }
}