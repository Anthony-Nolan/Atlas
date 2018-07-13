using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.SearchResults;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Services.Scoring
{
    public interface IDonorScoringService
    {
        Task<IEnumerable<MatchAndScoreResult>> Score(AlleleLevelMatchCriteria searchCriteria, IEnumerable<MatchResult> matchResults);
    }

    public class DonorScoringService : IDonorScoringService
    {
        private readonly IHlaScoringLookupService hlaScoringLookupService;

        // TODO:NOVA-930 inject dependencies
        public DonorScoringService(IHlaScoringLookupService hlaScoringLookupService)
        {
            this.hlaScoringLookupService = hlaScoringLookupService;
        }

        public Task<IEnumerable<MatchAndScoreResult>> Score(AlleleLevelMatchCriteria searchCriteria, IEnumerable<MatchResult> matchResults)
        {
            // TODO: NOVA-1449: (write tests and) implement
            return Task.FromResult(matchResults.Select(r => new MatchAndScoreResult
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