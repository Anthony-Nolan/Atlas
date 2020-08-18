using System.Collections.Generic;
using Atlas.Common.GeneticData;

namespace Atlas.Functions.PublicApi.Models.Search.Requests
{
    public class ScoringCriteria
    {
        /// <summary>
        /// By default, scoring is not performed on matched donor HLA, except on the loci specified here.
        /// </summary>
        public IEnumerable<Locus> LociToScore { get; set; }

        /// <summary>
        /// By default, the algorithm will use scoring information available at loci defined in <see cref="LociToScore"/>
        /// to aggregate into some overall values to use for ranking. e.g. MatchCategory, GradeScore, ConfidenceScore
        /// Any loci specified here can be excluded from these aggregates.
        /// </summary>
        public IEnumerable<Locus> LociToExcludeFromAggregateScore { get; set; }
    }

    public static class ScoringCriteriaMappings
    {
        public static MatchingAlgorithm.Client.Models.SearchRequests.ScoringCriteria ToMatchingAlgorithmScoringCriteria(
            this ScoringCriteria scoringCriteria)
        {
            return new MatchingAlgorithm.Client.Models.SearchRequests.ScoringCriteria
            {
                LociToScore = scoringCriteria?.LociToScore,
                LociToExcludeFromAggregateScore = scoringCriteria?.LociToExcludeFromAggregateScore
            };
        }
    }
}
