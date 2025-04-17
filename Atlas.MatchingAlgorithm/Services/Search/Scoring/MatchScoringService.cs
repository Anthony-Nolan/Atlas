using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Client.Models.Scoring;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;
using Atlas.MatchingAlgorithm.Helpers;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Aggregation;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.AntigenMatching;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Confidence;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Ranking;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring
{
    internal interface IMatchScoringService
    {
        Task<IEnumerable<MatchAndScoreResult>> StreamScoring(StreamingMatchResultsScoringRequest request);
        Task<IEnumerable<MatchAndScoreResult>> ScoreMatchesAgainstPatientHla(MatchResultsScoringRequest request);
    }

    internal class MatchScoringService : DonorScoringService, IMatchScoringService
    {
        private readonly IRankingService rankingService;
        private readonly IMatchingAlgorithmSearchLogger searchLogger;
        private readonly IMatchingAlgorithmSearchTrackingDispatcher matchingAlgorithmSearchTrackingDispatcher;

        public MatchScoringService(
            IHlaMetadataDictionaryFactory factory,
            IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor,
            IGradingService gradingService,
            IConfidenceService confidenceService,
            IAntigenMatchingService antigenMatchingService,
            IRankingService rankingService,
            IMatchScoreCalculator matchScoreCalculator,
            IScoreResultAggregator scoreResultAggregator,
            IMatchingAlgorithmSearchLogger searchLogger,
            IDpb1TceGroupMatchCalculator dpb1TceGroupMatchCalculator,
            ILogger logger,
            IMatchingAlgorithmSearchTrackingDispatcher matchingAlgorithmSearchTrackingDispatcher)
            : base(
                factory,
                hlaNomenclatureVersionAccessor,
                gradingService,
                confidenceService,
                antigenMatchingService,
                matchScoreCalculator,
                scoreResultAggregator,
                dpb1TceGroupMatchCalculator,
                logger)
        {
            this.rankingService = rankingService;
            this.searchLogger = searchLogger;
            this.matchingAlgorithmSearchTrackingDispatcher = matchingAlgorithmSearchTrackingDispatcher;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<MatchAndScoreResult>> StreamScoring(StreamingMatchResultsScoringRequest request)
        {
            using (searchLogger.RunTimed("Scoring"))
            {
                var scoredResults = StreamScoringUnranked(request);
                var reifiedScoredResults = await scoredResults.ToListAsync();
                return rankingService.RankSearchResults(reifiedScoredResults);
            }
        }

        public async Task<IEnumerable<MatchAndScoreResult>> ScoreMatchesAgainstPatientHla(MatchResultsScoringRequest request)
        {
            if (request.ScoringCriteria.LociToScore.IsNullOrEmpty())
            {
                return request.MatchResults.Select(m => new MatchAndScoreResult {MatchResult = m});
            }

            var patientScoringMetadata = await GetHlaScoringMetadata(request.PatientHla.ToPhenotypeInfo(), request.ScoringCriteria.LociToScore);

            var matchAndScoreResults = new List<MatchAndScoreResult>();
            foreach (var matchResult in request.MatchResults)
            {
                matchAndScoreResults.Add(new MatchAndScoreResult
                {
                    MatchResult = matchResult,
                    ScoreResult = await ScoreDonorHlaAgainstPatientMetadata(matchResult.DonorInfo.HlaNames, request.ScoringCriteria, patientScoringMetadata)
                });
            }

            return rankingService.RankSearchResults(matchAndScoreResults);
        }

        private async IAsyncEnumerable<MatchAndScoreResult> StreamScoringUnranked(StreamingMatchResultsScoringRequest request)
        {
            if (request.ScoringCriteria.LociToScore.IsNullOrEmpty())
            {
                await foreach (var result in request.MatchResults.SelectAsync(m => new MatchAndScoreResult {MatchResult = m}))
                {
                    yield return result;
                }
            }
            else
            {
                var patientScoringMetadata = await GetHlaScoringMetadata(request.PatientHla.ToPhenotypeInfo(), request.ScoringCriteria.LociToScore);
                var matchResultsCount = await request.MatchResults.CountAsync();
                await foreach (var matchResult in request.MatchResults)
                {
                    await matchingAlgorithmSearchTrackingDispatcher.ProcessCoreScoringOneDonorStarted();

                    yield return new MatchAndScoreResult
                    {
                        MatchResult = matchResult,
                        ScoreResult = await ScoreDonorHlaAgainstPatientMetadata(matchResult.DonorInfo.HlaNames, request.ScoringCriteria, patientScoringMetadata)
                    };
                }

                if (matchResultsCount > 0)
                {
                    await matchingAlgorithmSearchTrackingDispatcher.ProcessCoreScoringAllDonorsEnded();
                }
            }
        }
    }

    internal class MatchResultsScoringRequest : ScoringRequest
    {
        public IEnumerable<MatchResult> MatchResults { get; set; }
    }

    internal class StreamingMatchResultsScoringRequest : ScoringRequest
    {
        public IAsyncEnumerable<MatchResult> MatchResults { get; set; }
    }
}