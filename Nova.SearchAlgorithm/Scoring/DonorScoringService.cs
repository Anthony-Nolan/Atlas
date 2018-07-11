using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.SearchResults;

namespace Nova.SearchAlgorithm.Scoring
{
    public interface IDonorScoringService
    {
        Task<IEnumerable<SearchResult>> Score(AlleleLevelMatchCriteria searchCriteria, IEnumerable<MatchResult> matchResults);
    }

    public class DonorScoringService : IDonorScoringService
    {
        // TODO:NOVA-930 inject dependencies
        public DonorScoringService()
        {
        }

        public Task<IEnumerable<SearchResult>> Score(AlleleLevelMatchCriteria searchCriteria, IEnumerable<MatchResult> matchResults)
        {
            // TODO:NOVA-930 (write tests and) implement
            return Task.FromResult(matchResults.Select(r => new SearchResult
            {
                MatchResult = r, 
                ScoreResult = new ScoreResult
                {
                    ScoreDetailsAtLocusA = new LocusScoreDetails(),
                    ScoreDetailsAtLocusB = new LocusScoreDetails(),
                    ScoreDetailsAtLocusC = new LocusScoreDetails(),
                    ScoreDetailsAtLocusDqb1 = new LocusScoreDetails(),
                    ScoreDetailsAtLocusDrb1 = new LocusScoreDetails()
                }
            }));
        }
    }
}