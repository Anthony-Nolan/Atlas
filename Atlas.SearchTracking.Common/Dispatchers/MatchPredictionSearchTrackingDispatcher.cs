using Atlas.Common.ApplicationInsights;
using Atlas.SearchTracking.Common.ApplicationInsights;
using Atlas.SearchTracking.Common.Clients;
using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Common.Models;

namespace Atlas.SearchTracking.Common.Dispatchers
{
    public interface IMatchPredictionSearchTrackingDispatcher
    {
        Task ProcessInitiation(Guid searchIdentifier, Guid? originalSearchIdentifier, DateTime initiationTime, bool isParallelMatchPrediction, bool isPrecomputedMatchPrediction);

        Task ProcessPrepareBatchesStarted(Guid searchIdentifier, Guid? originalSearchIdentifier);

        Task ProcessPrepareBatchesEnded(Guid searchIdentifier, Guid? originalSearchIdentifier);

        Task ProcessRunningBatchesStarted(Guid searchIdentifier, Guid? originalSearchIdentifier);

        Task ProcessRunningBatchesEnded(Guid searchIdentifier, Guid? originalSearchIdentifier);

        Task ProcessPersistingResultsStarted(Guid searchIdentifier, Guid? originalSearchIdentifier);

        Task ProcessPersistingResultsEnded(Guid searchIdentifier, Guid? originalSearchIdentifier);

        Task ProcessCompleted((Guid SearchIdentifier, Guid? OriginalSearchIdentifier, bool IsSuccessful, MatchPredictionFailureInfo FailureInfo, int? DonorsPerBatch, int? TotalNumberOfBatches)
            eventDetails);

        Task ProcessResultsSent(Guid searchIdentifier, Guid? originalSearchIdentifier);
    }

    public class MatchPredictionSearchTrackingDispatcher(ISearchTrackingServiceBusClient searchTrackingServiceBusClient, IAtlasLogger logger)
        : IMatchPredictionSearchTrackingDispatcher
    {
        public async Task ProcessInitiation(Guid searchIdentifier, Guid? originalSearchIdentifier, DateTime initiationTime, bool isParallelMatchPrediction, bool isPrecomputedMatchPrediction)
        {
            var matchPredictionStartedEvent = new MatchPredictionStartedEvent
            {
                SearchIdentifier = searchIdentifier,
                OriginalSearchIdentifier = originalSearchIdentifier,
                InitiationTimeUtc = initiationTime,
                StartTimeUtc = DateTime.UtcNow,
                IsParallelMatchPrediction = isParallelMatchPrediction,
                IsPrecomputedMatchPrediction = isPrecomputedMatchPrediction
            };

            await Publish(matchPredictionStartedEvent, SearchTrackingEventType.MatchPredictionStarted);
        }

        public async Task ProcessPrepareBatchesStarted(Guid searchIdentifier, Guid? originalSearchIdentifier)
        {
            var matchPredictionTimingEvent = new MatchPredictionTimingEvent
            {
                SearchIdentifier = searchIdentifier,
                OriginalSearchIdentifier = originalSearchIdentifier,
                TimeUtc = DateTime.UtcNow
            };

            await Publish(matchPredictionTimingEvent, SearchTrackingEventType.MatchPredictionBatchPreparationStarted);
        }

        public async Task ProcessPrepareBatchesEnded(Guid searchIdentifier, Guid? originalSearchIdentifier)
        {
            var matchPredictionTimingEvent = new MatchPredictionTimingEvent
            {
                SearchIdentifier = searchIdentifier,
                OriginalSearchIdentifier = originalSearchIdentifier,
                TimeUtc = DateTime.UtcNow
            };

            await Publish(matchPredictionTimingEvent, SearchTrackingEventType.MatchPredictionBatchPreparationEnded);
        }

        public async Task ProcessRunningBatchesStarted(Guid searchIdentifier, Guid? originalSearchIdentifier)
        {
            var matchPredictionTimingEvent = new MatchPredictionTimingEvent
            {
                SearchIdentifier = searchIdentifier,
                OriginalSearchIdentifier = originalSearchIdentifier,
                TimeUtc = DateTime.UtcNow
            };

            await Publish(matchPredictionTimingEvent, SearchTrackingEventType.MatchPredictionRunningBatchesStarted);
        }

        public async Task ProcessRunningBatchesEnded(Guid searchIdentifier, Guid? originalSearchIdentifier)
        {
            var matchPredictionTimingEvent = new MatchPredictionTimingEvent
            {
                SearchIdentifier = searchIdentifier,
                OriginalSearchIdentifier = originalSearchIdentifier,
                TimeUtc = DateTime.UtcNow
            };

            await Publish(matchPredictionTimingEvent, SearchTrackingEventType.MatchPredictionRunningBatchesEnded);
        }

        public async Task ProcessPersistingResultsStarted(Guid searchIdentifier, Guid? originalSearchIdentifier)
        {
            var matchPredictionTimingEvent = new MatchPredictionTimingEvent
            {
                SearchIdentifier = searchIdentifier,
                OriginalSearchIdentifier = originalSearchIdentifier,
                TimeUtc = DateTime.UtcNow
            };

            await Publish(matchPredictionTimingEvent, SearchTrackingEventType.MatchPredictionPersistingResultsStarted);
        }

        public async Task ProcessPersistingResultsEnded(Guid searchIdentifier, Guid? originalSearchIdentifier)
        {
            var matchPredictionTimingEvent = new MatchPredictionTimingEvent
            {
                SearchIdentifier = searchIdentifier,
                OriginalSearchIdentifier = originalSearchIdentifier,
                TimeUtc = DateTime.UtcNow
            };

            await Publish(matchPredictionTimingEvent, SearchTrackingEventType.MatchPredictionPersistingResultsEnded);
        }

        public async Task ProcessCompleted((Guid SearchIdentifier, Guid? OriginalSearchIdentifier, bool IsSuccessful, MatchPredictionFailureInfo FailureInfo, int? DonorsPerBatch, int? TotalNumberOfBatches) eventDetails)
        {
            var matchPredictionCompletedEvent = new MatchPredictionCompletedEvent
            {
                SearchIdentifier = eventDetails.SearchIdentifier,
                OriginalSearchIdentifier = eventDetails.OriginalSearchIdentifier,
                CompletionTimeUtc = DateTime.UtcNow,
                CompletionDetails = new MatchPredictionCompletionDetails
                {
                    IsSuccessful = eventDetails.IsSuccessful,
                    FailureInfo = eventDetails.FailureInfo,
                    DonorsPerBatch = eventDetails.DonorsPerBatch,
                    TotalNumberOfBatches = eventDetails.TotalNumberOfBatches,
                }
            };

            await Publish(matchPredictionCompletedEvent, SearchTrackingEventType.MatchPredictionCompleted);
        }

        public async Task ProcessResultsSent(Guid searchIdentifier, Guid? originalSearchIdentifier)
        {
            var matchPredictionTimingEvent = new MatchPredictionTimingEvent
            {
                SearchIdentifier = searchIdentifier,
                OriginalSearchIdentifier = originalSearchIdentifier,
                TimeUtc = DateTime.UtcNow
            };

            await Publish(matchPredictionTimingEvent, SearchTrackingEventType.MatchPredictionResultsSent);
        }

        // Sent to Application Insights first, so that a Service Bus publish that fails after its retries does not lose the record.
        private async Task Publish<TEvent>(TEvent searchTrackingEvent, SearchTrackingEventType eventType) where TEvent : ISearchTrackingEvent
        {
            logger.SendSearchTrackingEvent(searchTrackingEvent, eventType);
            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(searchTrackingEvent, eventType);
        }
    }
}
