using System.Threading.Tasks;
using System;
using Atlas.SearchTracking.Common.Clients;
using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Common.Models;

namespace Atlas.Functions.Services
{
    public interface IMatchPredictionSearchTrackingDispatcher
    {
        Task ProcessInitiation(Guid searchRequestId, DateTime initiationTime);

        Task ProcessPrepareBatchesStarted(Guid searchRequestId);

        Task ProcessPrepareBatchesEnded(Guid searchRequestId);

        Task ProcessRunningBatchesStarted(Guid searchRequestId);

        Task ProcessRunningBatchesEnded(Guid searchRequestId);

        Task ProcessPersistingResultsStarted(Guid searchRequestId);

        Task ProcessPersistingResultsEnded(Guid searchRequestId);

        Task ProcessCompleted((Guid SearchIdentifier, MatchPredictionFailureInfo FailureInfo, int? DonorsPerBatch, int? TotalNumberOfBatches)
            eventDetails);
    }

    public class MatchPredictionSearchTrackingDispatcher(ISearchTrackingServiceBusClient searchTrackingServiceBusClient)
        : IMatchPredictionSearchTrackingDispatcher
    {
        public async Task ProcessInitiation(Guid searchRequestId, DateTime initiationTime)
        {
            var matchPredictionStartedEvent = new MatchPredictionStartedEvent
            {
                SearchIdentifier = searchRequestId,
                InitiationTimeUtc = initiationTime,
                StartTimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchPredictionStartedEvent, SearchTrackingEventType.MatchPredictionStarted);
        }

        public async Task ProcessPrepareBatchesStarted(Guid searchRequestId)
        {
            var matchPredictionTimingEvent = new MatchPredictionTimingEvent()
            {
                SearchIdentifier = searchRequestId,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchPredictionTimingEvent, SearchTrackingEventType.MatchPredictionBatchPreparationStarted);
        }

        public async Task ProcessPrepareBatchesEnded(Guid searchRequestId)
        {
            var matchPredictionTimingEvent = new MatchPredictionTimingEvent()
            {
                SearchIdentifier = searchRequestId,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchPredictionTimingEvent, SearchTrackingEventType.MatchPredictionBatchPreparationEnded);
        }

        public async Task ProcessPersistingResultsStarted(Guid searchRequestId)
        {
            var matchPredictionTimingEvent = new MatchPredictionTimingEvent()
            {
                SearchIdentifier = searchRequestId,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchPredictionTimingEvent, SearchTrackingEventType.MatchPredictionPersistingResultsStarted);
        }

        public async Task ProcessPersistingResultsEnded(Guid searchRequestId)
        {
            var matchPredictionTimingEvent = new MatchPredictionTimingEvent()
            {
                SearchIdentifier = searchRequestId,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchPredictionTimingEvent, SearchTrackingEventType.MatchPredictionPersistingResultsEnded);
        }

        public async Task ProcessRunningBatchesStarted(Guid searchRequestId)
        {
            var matchPredictionTimingEvent = new MatchPredictionTimingEvent()
            {
                SearchIdentifier = searchRequestId,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchPredictionTimingEvent, SearchTrackingEventType.MatchPredictionRunningBatchesStarted);
        }

        public async Task ProcessRunningBatchesEnded(Guid searchRequestId)
        {
            var matchPredictionTimingEvent = new MatchPredictionTimingEvent()
            {
                SearchIdentifier = searchRequestId,
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
