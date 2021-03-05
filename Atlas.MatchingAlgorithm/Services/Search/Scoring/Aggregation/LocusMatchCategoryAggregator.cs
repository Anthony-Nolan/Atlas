using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring.Aggregation
{
    internal static class LocusMatchCategoryAggregator
    {
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
    }
}