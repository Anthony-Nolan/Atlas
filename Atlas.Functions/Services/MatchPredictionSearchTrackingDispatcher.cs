using System.Threading.Tasks;
using System;
using Atlas.SearchTracking.Common.Clients;
using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Common.Models;

namespace Atlas.Functions.Services
{
    public interface IMatchPredictionSearchTrackingDispatcher
    {
        Task ProcessInitiation(Guid searchIdentifier, DateTime initiationTime);

        Task ProcessPrepareBatchesStarted(Guid searchIdentifier);

        Task ProcessPrepareBatchesEnded(Guid searchIdentifier);

        Task ProcessRunningBatchesStarted(Guid searchIdentifier);

        Task ProcessRunningBatchesEnded(Guid searchIdentifier);

        Task ProcessPersistingResultsStarted(Guid searchIdentifier);

        Task ProcessPersistingResultsEnded(Guid searchIdentifier);

        Task ProcessCompleted((Guid SearchIdentifier, MatchPredictionFailureInfo FailureInfo, int? DonorsPerBatch, int? TotalNumberOfBatches)
            eventDetails);
    }

    public class MatchPredictionSearchTrackingDispatcher(ISearchTrackingServiceBusClient searchTrackingServiceBusClient)
        : IMatchPredictionSearchTrackingDispatcher
    {
        public async Task ProcessInitiation(Guid searchIdentifier, DateTime initiationTime)
        {
            var matchPredictionStartedEvent = new MatchPredictionStartedEvent
            {
                SearchIdentifier = searchIdentifier,
                InitiationTimeUtc = initiationTime,
                StartTimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchPredictionStartedEvent, SearchTrackingEventType.MatchPredictionStarted);
        }

        public async Task ProcessPrepareBatchesStarted(Guid searchIdentifier)
        {
            var matchPredictionTimingEvent = new MatchPredictionTimingEvent()
            {
                SearchIdentifier = searchIdentifier,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchPredictionTimingEvent, SearchTrackingEventType.MatchPredictionBatchPreparationStarted);
        }

        public async Task ProcessPrepareBatchesEnded(Guid searchIdentifier)
        {
            var matchPredictionTimingEvent = new MatchPredictionTimingEvent()
            {
                SearchIdentifier = searchIdentifier,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchPredictionTimingEvent, SearchTrackingEventType.MatchPredictionBatchPreparationEnded);
        }

        public async Task ProcessPersistingResultsStarted(Guid searchIdentifier)
        {
            var matchPredictionTimingEvent = new MatchPredictionTimingEvent()
            {
                SearchIdentifier = searchIdentifier,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchPredictionTimingEvent, SearchTrackingEventType.MatchPredictionPersistingResultsStarted);
        }

        public async Task ProcessPersistingResultsEnded(Guid searchIdentifier)
        {
            var matchPredictionTimingEvent = new MatchPredictionTimingEvent()
            {
                SearchIdentifier = searchIdentifier,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchPredictionTimingEvent, SearchTrackingEventType.MatchPredictionPersistingResultsEnded);
        }

        public async Task ProcessRunningBatchesStarted(Guid searchIdentifier)
        {
            var matchPredictionTimingEvent = new MatchPredictionTimingEvent()
            {
                SearchIdentifier = searchIdentifier,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchPredictionTimingEvent, SearchTrackingEventType.MatchPredictionRunningBatchesStarted);
        }

        public async Task ProcessRunningBatchesEnded(Guid searchIdentifier)
        {
            var matchPredictionTimingEvent = new MatchPredictionTimingEvent()
            {
                SearchIdentifier = searchIdentifier,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchPredictionTimingEvent, SearchTrackingEventType.MatchPredictionRunningBatchesEnded);
        }

        public async Task ProcessCompleted((Guid SearchIdentifier, MatchPredictionFailureInfo FailureInfo, int? DonorsPerBatch, int? TotalNumberOfBatches) eventDetails)
        {
            var matchPredictionCompletedEvent = new MatchPredictionCompletedEvent
            {
                SearchIdentifier = eventDetails.SearchIdentifier,
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
    }
}
