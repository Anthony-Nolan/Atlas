using System.Collections.Generic;
using System.Linq;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring.Ranking
{
    public interface IRankingService
    {
        /// <summary>
        /// Combines match count with scores for match grade and confidence, to order the search results
        /// </summary>
        IEnumerable<MatchAndScoreResult> RankSearchResults(IEnumerable<MatchAndScoreResult> results);
    }

    public class RankingService : IRankingService
    {
        public IEnumerable<MatchAndScoreResult> RankSearchResults(IEnumerable<MatchAndScoreResult> results)
        {
            return results
                .OrderByDescending(r => r.ScoreResult?.AggregateScoreDetails?.MatchCount)
                .ThenByDescending(r => r.ScoreResult?.AggregateScoreDetails?.GradeScore)
                .ThenByDescending(r => r.ScoreResult?.AggregateScoreDetails?.ConfidenceScore);
        }
    }
}