using Atlas.SearchTracking.Data.Repositories;
using Newtonsoft.Json;
using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Common.Models;

namespace Atlas.SearchTracking.Services
{
    public interface ISearchTrackingEventProcessor
    {
        public Task HandleEvent(string body, SearchTrackingEventType eventType);
    }

    public class SearchTrackingEventProcessor : ISearchTrackingEventProcessor
    {
        private readonly ISearchRequestRepository searchRequestRepository;
        private readonly IMatchPredictionRepository matchPredictionRepository;
        private readonly ISearchRequestMatchingAlgorithmAttemptsRepository searchRequestMatchingAlgorithmAttemptsRepository;

        public SearchTrackingEventProcessor(ISearchRequestRepository searchRequestRepository,
            IMatchPredictionRepository matchPredictionRepository,
            ISearchRequestMatchingAlgorithmAttemptsRepository searchRequestMatchingAlgorithmAttemptsRepository)
        {
            this.searchRequestRepository = searchRequestRepository;
            this.matchPredictionRepository = matchPredictionRepository;
            this.searchRequestMatchingAlgorithmAttemptsRepository = searchRequestMatchingAlgorithmAttemptsRepository;
        }

        public async Task HandleEvent(string body, SearchTrackingEventType eventType)
        {
           switch (eventType)
            {
                case SearchTrackingEventType.SearchRequested:
                    var searchRequestedEvent = JsonConvert.DeserializeObject<SearchRequestedEvent>(body);
                    await searchRequestRepository.TrackSearchRequestedEvent(searchRequestedEvent);
                    break;
                case SearchTrackingEventType.SearchRequestCompleted:
                    var searchRequestCompletedEvent = JsonConvert.DeserializeObject<SearchRequestCompletedEvent>(body);
                    await searchRequestRepository.TrackSearchRequestCompletedEvent(searchRequestCompletedEvent);
                    break;
                case SearchTrackingEventType.MatchingAlgorithmAttemptStarted:
                    var matchingAlgorithmAttemptStartedEvent = JsonConvert.DeserializeObject<MatchingAlgorithmAttemptStartedEvent>(body);
                    await searchRequestMatchingAlgorithmAttemptsRepository.TrackStartedEvent(matchingAlgorithmAttemptStartedEvent);
                    break;
                case SearchTrackingEventType.MatchingAlgorithmCoreMatchingEnded or
                    SearchTrackingEventType.MatchingAlgorithmCoreMatchingStarted or
                    SearchTrackingEventType.MatchingAlgorithmCoreScoringEnded or
                    SearchTrackingEventType.MatchingAlgorithmCoreScoringStarted or
                    SearchTrackingEventType.MatchingAlgorithmPersistingResultsEnded or
                    SearchTrackingEventType.MatchingAlgorithmPersistingResultsStarted:
                    var matchingAlgorithmTimingEvent = JsonConvert.DeserializeObject<MatchingAlgorithmAttemptTimingEvent>(body);
                    await searchRequestMatchingAlgorithmAttemptsRepository.TrackTimingEvent(matchingAlgorithmTimingEvent, eventType);
                    break;
                case
                    SearchTrackingEventType.MatchingAlgorithmCompleted:
                    var matchingAlgorithmCompletedEvent = JsonConvert.DeserializeObject<MatchingAlgorithmCompletedEvent>(body);
                    await searchRequestRepository.TrackMatchingAlgorithmCompletedEvent(matchingAlgorithmCompletedEvent);
                    await searchRequestMatchingAlgorithmAttemptsRepository.TrackCompletedEvent(matchingAlgorithmCompletedEvent);
                    break;
                case SearchTrackingEventType.MatchPredictionStarted:
                    var matchPredictionStartedEvent = JsonConvert.DeserializeObject<MatchPredictionStartedEvent>(body);
                    await matchPredictionRepository.TrackStartedEvent(matchPredictionStartedEvent);
                    break;
                case SearchTrackingEventType.MatchPredictionBatchPreparationEnded or
                    SearchTrackingEventType.MatchPredictionBatchPreparationStarted or
                    SearchTrackingEventType.MatchPredictionRunningBatchesEnded or
                    SearchTrackingEventType.MatchPredictionRunningBatchesStarted or
                    SearchTrackingEventType.MatchPredictionPersistingResultsEnded or
                    SearchTrackingEventType.MatchPredictionPersistingResultsStarted:
                    var matchPredictionTimingEvent = JsonConvert.DeserializeObject<MatchPredictionTimingEvent>(body);
                    await matchPredictionRepository.TrackTimingEvent(matchPredictionTimingEvent, eventType);
                    break;
                case
                    SearchTrackingEventType.MatchPredictionCompleted:
                    var matchPredictionCompletedEvent = JsonConvert.DeserializeObject<MatchPredictionCompletedEvent>(body);
                    await searchRequestRepository.TrackMatchPredictionCompletedEvent(matchPredictionCompletedEvent);
                    await matchPredictionRepository.TrackCompletedEvent(matchPredictionCompletedEvent);
                    break;
                case SearchTrackingEventType.MatchPredictionResultsSent:
                    var matchPredictionResultsSentEvent = JsonConvert.DeserializeObject<MatchPredictionResultsSentEvent>(body);
                    await searchRequestRepository.TrackMatchPredictionResultsSentEvent(matchPredictionResultsSentEvent);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(eventType));
            }
        }
    }
}
