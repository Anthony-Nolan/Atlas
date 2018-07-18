using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.SearchResults;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.MatchingDictionaryConversions;
using Nova.SearchAlgorithm.Services.Scoring.Ranking;

namespace Nova.SearchAlgorithm.Services.Scoring
{
    public interface IDonorScoringService
    {
        Task<IEnumerable<MatchAndScoreResult>> Score(PhenotypeInfo<string> patientHla, IEnumerable<MatchResult> matchResults);
    }

    public class DonorScoringService : IDonorScoringService
    {
        private readonly IHlaScoringLookupService hlaScoringLookupService;
        private readonly IGradingService gradingService;
        private readonly IConfidenceService confidenceService;
        private readonly IRankingService rankingService;

        public DonorScoringService(
            IHlaScoringLookupService hlaScoringLookupService,
            IGradingService gradingService,
            IConfidenceService confidenceService,
            IRankingService rankingService
        )
        {
            this.hlaScoringLookupService = hlaScoringLookupService;
            this.gradingService = gradingService;
            this.confidenceService = confidenceService;
            this.rankingService = rankingService;
        }

        public async Task<IEnumerable<MatchAndScoreResult>> Score(PhenotypeInfo<string> patientHla, IEnumerable<MatchResult> matchResults)
        {
            var patientScoringLookupResults = patientHla.MapAsync(async (locus, position, hla) => await GetHlaScoringResultsForLocus(locus, hla));

            var donorScoringLookupResults = matchResults.Select(matchResult =>
            {
                return matchResult.Donor.HlaNames.MapAsync(async (locus, position, hla) => await GetHlaScoringResultsForLocus(locus, hla));
            }).ToList();

            // TODO: NOVA-1449: (write tests and) implement
            return await Task.FromResult(matchResults.Select(r => new MatchAndScoreResult
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

        private async Task<IHlaScoringLookupResult> GetHlaScoringResultsForLocus(Locus locus, string hla)
        {
            if (locus == Locus.Dpb1 || hla == null)
            {
                return null;
            }

            return await hlaScoringLookupService.GetHlaScoringLookupResults(locus.ToMatchLocus(), hla);
        }
    }
}