using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.SearchResults;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.MatchingDictionaryConversions;

namespace Nova.SearchAlgorithm.Services.Scoring
{
    public interface IDonorScoringService
    {
        Task<IEnumerable<MatchAndScoreResult>> Score(AlleleLevelMatchCriteria searchCriteria, IEnumerable<MatchResult> matchResults);
    }

    public class DonorScoringService : IDonorScoringService
    {
        private readonly IMatchingDictionaryLookupService matchingDictionaryLookupService;

        // TODO:NOVA-930 inject dependencies
        public DonorScoringService(IMatchingDictionaryLookupService matchingDictionaryLookupService)
        {
            this.matchingDictionaryLookupService = matchingDictionaryLookupService;
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