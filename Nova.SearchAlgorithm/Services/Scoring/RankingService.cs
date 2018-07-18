using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Common.Models.SearchResults;

namespace Nova.SearchAlgorithm.Services.Scoring
{
    public interface IRankingService
    {
        IEnumerable<MatchAndScoreResult> RankSearchResults(IEnumerable<MatchAndScoreResult> results);
    }

    public class RankingService : IRankingService
    {
        public IEnumerable<MatchAndScoreResult> RankSearchResults(IEnumerable<MatchAndScoreResult> results)
        {
            return results
                .OrderByDescending(r => r.MatchResult.TotalMatchCount)
                .ThenByDescending(r => r.ScoreResult.TotalMatchGradeScore);
        }
    }
}