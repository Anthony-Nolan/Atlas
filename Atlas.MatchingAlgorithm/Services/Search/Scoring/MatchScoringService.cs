using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Client.Models.Scoring;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;
using Atlas.MatchingAlgorithm.Helpers;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Aggregation;
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

        public MatchScoringService(
            IHlaMetadataDictionaryFactory factory,
            IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor,
            IGradingService gradingService,
            IConfidenceService confidenceService,
            IRankingService rankingService,
            IMatchScoreCalculator matchScoreCalculator,
            IScoreResultAggregator scoreResultAggregator,
            IMatchingAlgorithmSearchLogger searchLogger,
            IDpb1TceGroupMatchCalculator dpb1TceGroupMatchCalculator)
            : base(
                factory,
                hlaNomenclatureVersionAccessor,
                gradingService,
                confidenceService,
                matchScoreCalculator,
                scoreResultAggregator,
                dpb1TceGroupMatchCalculator)
        {
            this.rankingService = rankingService;
            this.searchLogger = searchLogger;
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
                    ScoreResult = await ScoreDonorHlaAgainstPatientMetadata(matchResult.DonorInfo.HlaNames, request, patientScoringMetadata)
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
                await foreach (var matchResult in request.MatchResults)
                {
                    yield return new MatchAndScoreResult
                    {
                        MatchResult = matchResult,
                        ScoreResult = await ScoreDonorHlaAgainstPatientMetadata(matchResult.DonorInfo.HlaNames, request, patientScoringMetadata)
                    };
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