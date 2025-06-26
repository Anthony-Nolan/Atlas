using System.Threading.Tasks;
using System;
using Atlas.SearchTracking.Common.Clients;
using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Common.Models;

namespace Atlas.Functions.Services
{
    public interface IMatchPredictionSearchTrackingDispatcher
    {
        Task ProcessInitiation(Guid searchIdentifier, Guid? originalSearchIdentifier, DateTime initiationTime);

        Task ProcessPrepareBatchesStarted(Guid searchIdentifier, Guid? originalSearchIdentifier);

        Task ProcessPrepareBatchesEnded(Guid searchIdentifier, Guid? originalSearchIdentifier);

        Task ProcessRunningBatchesStarted(Guid searchIdentifier, Guid? originalSearchIdentifier);

        Task ProcessRunningBatchesEnded(Guid searchIdentifier, Guid? originalSearchIdentifier);

        Task ProcessPersistingResultsStarted(Guid searchIdentifier, Guid? originalSearchIdentifier);

        Task ProcessPersistingResultsEnded(Guid searchIdentifier, Guid? originalSearchIdentifier);

        Task ProcessCompleted((Guid SearchIdentifier, Guid? OriginalSearchIdentifier, MatchPredictionFailureInfo FailureInfo, int? DonorsPerBatch, int? TotalNumberOfBatches)
            eventDetails);
        
        Task ProcessResultsSent(Guid searchIdentifier, Guid? originalSearchIdentifier);
    }

    public class MatchPredictionSearchTrackingDispatcher(ISearchTrackingServiceBusClient searchTrackingServiceBusClient)
        : IMatchPredictionSearchTrackingDispatcher
    {
        public async Task ProcessInitiation(Guid searchIdentifier, Guid? originalSearchIdentifier, DateTime initiationTime)
        {
            var matchPredictionStartedEvent = new MatchPredictionStartedEvent
            {
                SearchIdentifier = searchIdentifier,
                OriginalSearchIdentifier = originalSearchIdentifier,
                InitiationTimeUtc = initiationTime,
                StartTimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchPredictionStartedEvent, SearchTrackingEventType.MatchPredictionStarted);
        }

        public async Task ProcessPrepareBatchesStarted(Guid searchIdentifier, Guid? originalSearchIdentifier)
        {
            var matchPredictionTimingEvent = new MatchPredictionTimingEvent
            {
                SearchIdentifier = searchIdentifier,
                OriginalSearchIdentifier = originalSearchIdentifier,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchPredictionTimingEvent, SearchTrackingEventType.MatchPredictionBatchPreparationStarted);
        }

        public async Task ProcessPrepareBatchesEnded(Guid searchIdentifier, Guid? originalSearchIdentifier)
        {
            var matchPredictionTimingEvent = new MatchPredictionTimingEvent
            {
                SearchIdentifier = searchIdentifier,
                OriginalSearchIdentifier = originalSearchIdentifier,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchPredictionTimingEvent, SearchTrackingEventType.MatchPredictionBatchPreparationEnded);
        }

        public async Task ProcessPersistingResultsStarted(Guid searchIdentifier, Guid? originalSearchIdentifier)
        {
            var matchPredictionTimingEvent = new MatchPredictionTimingEvent
            {
                SearchIdentifier = searchIdentifier,
                OriginalSearchIdentifier = originalSearchIdentifier,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchPredictionTimingEvent, SearchTrackingEventType.MatchPredictionPersistingResultsStarted);
        }

        public async Task ProcessPersistingResultsEnded(Guid searchIdentifier, Guid? originalSearchIdentifier)
        {
            var matchPredictionTimingEvent = new MatchPredictionTimingEvent
            {
                SearchIdentifier = searchIdentifier,
                OriginalSearchIdentifier = originalSearchIdentifier,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchPredictionTimingEvent, SearchTrackingEventType.MatchPredictionPersistingResultsEnded);
        }

        public async Task ProcessRunningBatchesStarted(Guid searchIdentifier, Guid? originalSearchIdentifier)
        {
            var matchPredictionTimingEvent = new MatchPredictionTimingEvent
            {
                SearchIdentifier = searchIdentifier,
                OriginalSearchIdentifier = originalSearchIdentifier,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchPredictionTimingEvent, SearchTrackingEventType.MatchPredictionRunningBatchesStarted);
        }

        public async Task ProcessRunningBatchesEnded(Guid searchIdentifier, Guid? originalSearchIdentifier)
        {
            var matchPredictionTimingEvent = new MatchPredictionTimingEvent
            {
                SearchIdentifier = searchIdentifier,
                OriginalSearchIdentifier = originalSearchIdentifier,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchPredictionTimingEvent, SearchTrackingEventType.MatchPredictionRunningBatchesEnded);
        }

        public async Task ProcessCompleted((Guid SearchIdentifier, Guid? OriginalSearchIdentifier, MatchPredictionFailureInfo FailureInfo, int? DonorsPerBatch, int? TotalNumberOfBatches) eventDetails)
        {
            var matchPredictionCompletedEvent = new MatchPredictionCompletedEvent
            {
                SearchIdentifier = eventDetails.SearchIdentifier,
                OriginalSearchIdentifier = eventDetails.OriginalSearchIdentifier,
                CompletionTimeUtc = DateTime.UtcNow,
                CompletionDetails = new MatchPredictionCompletionDetails
                {
                    IsSuccessful = eventDetails.FailureInfo == null,
                    FailureInfo = eventDetails.FailureInfo,
                    DonorsPerBatch = eventDetails.DonorsPerBatch,
                    TotalNumberOfBatches = eventDetails.TotalNumberOfBatches,
                }
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchPredictionCompletedEvent, SearchTrackingEventType.MatchPredictionCompleted);
        }

        public async Task ProcessResultsSent(Guid searchIdentifier, Guid? originalSearchIdentifier)
        {
            var matchPredictionTimingEvent = new MatchPredictionTimingEvent
            {
                SearchIdentifier = searchIdentifier,
                OriginalSearchIdentifier = originalSearchIdentifier,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchPredictionTimingEvent, SearchTrackingEventType.MatchPredictionResultsSent);
        }
    }
}
