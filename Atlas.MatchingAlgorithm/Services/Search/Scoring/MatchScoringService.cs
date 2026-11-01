using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
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
        private readonly ISearchTrackingEventPublisher searchTrackingEventPublisher;

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
            IAtlasLogger logger,
            ISearchTrackingEventPublisher searchTrackingEventPublisher)
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
            this.searchTrackingEventPublisher = searchTrackingEventPublisher;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<MatchAndScoreResult>> StreamScoring(StreamingMatchResultsScoringRequest request)
        {
            using (searchLogger.RunTimed("Scoring"))
            {
                using var collection = ScoringStageTimings.Begin();
                var distinctDonorPhenotypes = new HashSet<PhenotypeInfo<string>>();

                var scoredResults = StreamScoringUnranked(request, distinctDonorPhenotypes);
                var reifiedScoredResults = await scoredResults.ToListAsync();

                IEnumerable<MatchAndScoreResult> ranked;
                using (ScoringStageTimings.Time(ScoringStage.Ranking))
                {
                    ranked = rankingService.RankSearchResults(reifiedScoredResults).ToList();
                }

                collection.Timings.Send(searchLogger, "Search", distinctDonorPhenotypes.Count, request.ScoringCriteria.LociToScore);
                return ranked;
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

        private async IAsyncEnumerable<MatchAndScoreResult> StreamScoringUnranked(
            StreamingMatchResultsScoringRequest request,
            HashSet<PhenotypeInfo<string>> distinctDonorPhenotypes)
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
                PhenotypeInfo<IHlaScoringMetadata> patientScoringMetadata;
                using (ScoringStageTimings.Time(ScoringStage.PatientMetadataLookup))
                {
                    patientScoringMetadata = await GetHlaScoringMetadata(request.PatientHla.ToPhenotypeInfo(), request.ScoringCriteria.LociToScore);
                }

                int matchResultsCount;
                using (ScoringStageTimings.Time(ScoringStage.DuplicateMatchCount))
                {
                    matchResultsCount = await request.MatchResults.CountAsync();
                }

                // A hand-written loop instead of await foreach, so the wait for the next match (matching time) is timed apart from scoring.
                await using var matches = request.MatchResults.GetAsyncEnumerator();
                while (true)
                {
                    bool hasNext;
                    using (ScoringStageTimings.Time(ScoringStage.MatchStreamWait))
                    {
                        hasNext = await matches.MoveNextAsync();
                    }

                    if (!hasNext)
                    {
                        break;
                    }

                    var matchResult = matches.Current;
                    using (ScoringStageTimings.Time(ScoringStage.SearchTrackingPublish))
                    {
                        await searchTrackingEventPublisher.ProcessCoreScoringOneDonorStarted();
                    }

                    distinctDonorPhenotypes.Add(matchResult.DonorInfo.HlaNames);

                    yield return new MatchAndScoreResult
                    {
                        MatchResult = matchResult,
                        ScoreResult = await ScoreDonorHlaAgainstPatientMetadata(matchResult.DonorInfo.HlaNames, request.ScoringCriteria, patientScoringMetadata)
                    };
                }

                if (matchResultsCount > 0)
                {
                    await searchTrackingEventPublisher.ProcessCoreScoringAllDonorsEnded();
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