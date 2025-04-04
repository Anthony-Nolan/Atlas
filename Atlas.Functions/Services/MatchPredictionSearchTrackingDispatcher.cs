using System.Threading.Tasks;
using System;
using Atlas.SearchTracking.Common.Clients;
using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Common.Models;
using Polly;

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

        Task ProcessCompleted((Guid SearchIdentifier, DateTime CompletionTimeUtc, MatchPredictionFailureInfo FailureInfo, int? DonorsPerBatch, int? TotalNumberOfBatches) matchPredictionCompletedData);
    }

    public class MatchPredictionSearchTrackingDispatcher(ISearchTrackingServiceBusClient searchTrackingServiceBusClient)
        : IMatchPredictionSearchTrackingDispatcher
    {
        public async Task ProcessInitiation(Guid searchRequestId, DateTime initiationTime)
        {
            var matchPredictionStartedEvent = new MatchPredictionStartedEvent
            {
                SearchRequestId = searchRequestId,
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
                SearchRequestId = searchRequestId,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchPredictionTimingEvent, SearchTrackingEventType.MatchPredictionBatchPreparationStarted);
        }

        public async Task ProcessPrepareBatchesEnded(Guid searchRequestId)
        {
            var matchPredictionTimingEvent = new MatchPredictionTimingEvent()
            {
                SearchRequestId = searchRequestId,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchPredictionTimingEvent, SearchTrackingEventType.MatchPredictionBatchPreparationEnded);
        }

        public async Task ProcessPersistingResultsStarted(Guid searchRequestId)
        {
            var matchPredictionTimingEvent = new MatchPredictionTimingEvent()
            {
                SearchRequestId = searchRequestId,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchPredictionTimingEvent, SearchTrackingEventType.MatchPredictionPersistingResultsStarted);
        }

        public async Task ProcessPersistingResultsEnded(Guid searchRequestId)
        {
            var matchPredictionTimingEvent = new MatchPredictionTimingEvent()
            {
                SearchRequestId = searchRequestId,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchPredictionTimingEvent, SearchTrackingEventType.MatchPredictionPersistingResultsEnded);
        }

        public async Task ProcessRunningBatchesStarted(Guid searchRequestId)
        {
            var matchPredictionTimingEvent = new MatchPredictionTimingEvent()
            {
                SearchRequestId = searchRequestId,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchPredictionTimingEvent, SearchTrackingEventType.MatchPredictionRunningBatchesStarted);
        }

        public async Task ProcessRunningBatchesEnded(Guid searchRequestId)
        {
            var matchPredictionTimingEvent = new MatchPredictionTimingEvent()
            {
                SearchRequestId = searchRequestId,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchPredictionTimingEvent, SearchTrackingEventType.MatchPredictionRunningBatchesEnded);
        }

        public async Task ProcessCompleted((Guid SearchIdentifier, DateTime CompletionTimeUtc, MatchPredictionFailureInfo FailureInfo, int? DonorsPerBatch, int? TotalNumberOfBatches) matchPredictionCompletedData)
        {
            var matchPredictionCompletedEvent = new MatchPredictionCompletedEvent
            {
                SearchRequestId = matchPredictionCompletedData.SearchIdentifier,
                CompletionTimeUtc = matchPredictionCompletedData.CompletionTimeUtc,
                CompletionDetails = new MatchPredictionCompletionDetails
                {
                    IsSuccessful = matchPredictionCompletedData.FailureInfo == null,
                    FailureInfo = matchPredictionCompletedData.FailureInfo,
                    DonorsPerBatch = matchPredictionCompletedData.DonorsPerBatch,
                    TotalNumberOfBatches = matchPredictionCompletedData.TotalNumberOfBatches,
                }
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchPredictionCompletedEvent, SearchTrackingEventType.MatchPredictionCompleted);
        }
    }
}
