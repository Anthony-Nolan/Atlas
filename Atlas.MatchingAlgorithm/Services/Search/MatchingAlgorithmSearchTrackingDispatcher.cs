﻿using System;
using System.Threading.Tasks;
using Atlas.SearchTracking.Common.Clients;
using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Common.Models;

namespace Atlas.MatchingAlgorithm.Services.Search
{
    public interface IMatchingAlgorithmSearchTrackingDispatcher
    {
        Task ProcessInitiation(DateTime initiationTime, DateTime startTime);

        Task ProcessCoreMatchingStarted();

        Task ProcessCoreMatchingEnded();

        Task ProcessCoreScoringOneDonorStarted();

        Task ProcessCoreScoringAllDonorsEnded();

        Task ProcessPersistingResultsStarted();

        Task ProcessPersistingResultsEnded();

        Task ProcessCompleted(MatchingAlgorithmCompletedEvent matchingAlgorithmCompletedEvent);
    }

    public class MatchingAlgorithmSearchTrackingDispatcher(
        IMatchingAlgorithmSearchTrackingContextManager matchingAlgorithmSearchTrackingContextManager,
        ISearchTrackingServiceBusClient searchTrackingServiceBusClient)
        : IMatchingAlgorithmSearchTrackingDispatcher
    {

        private bool scoringOneDonorStartedTriggered = false;

        public async Task ProcessInitiation(DateTime initiationTime, DateTime startTime)
        {
            var currentContext = matchingAlgorithmSearchTrackingContextManager.Retrieve();

            var matchingAlgorithmAttemptStartedEvent = new MatchingAlgorithmAttemptStartedEvent
            {
                SearchRequestId = currentContext.SearchRequestId,
                AttemptNumber = currentContext.AttemptNumber,
                InitiationTimeUtc = initiationTime,
                StartTimeUtc = startTime
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchingAlgorithmAttemptStartedEvent, SearchTrackingEventType.MatchingAlgorithmAttemptStarted);
        }

        public async Task ProcessCoreMatchingStarted()
        {
            var currentContext = matchingAlgorithmSearchTrackingContextManager.Retrieve();

            var matchingAlgorithmAttemptTimingEvent = new MatchingAlgorithmAttemptTimingEvent
            {
                SearchRequestId = currentContext.SearchRequestId,
                AttemptNumber = currentContext.AttemptNumber,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchingAlgorithmAttemptTimingEvent, SearchTrackingEventType.MatchingAlgorithmCoreMatchingStarted);
        }

        public async Task ProcessCoreMatchingEnded()
        {
            var currentContext = matchingAlgorithmSearchTrackingContextManager.Retrieve();

            var matchingAlgorithmAttemptTimingEvent = new MatchingAlgorithmAttemptTimingEvent
            {
                SearchRequestId = currentContext.SearchRequestId,
                AttemptNumber = currentContext.AttemptNumber,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchingAlgorithmAttemptTimingEvent, SearchTrackingEventType.MatchingAlgorithmCoreMatchingEnded);
        }

        public async Task ProcessCoreScoringOneDonorStarted()
        {
            if (!scoringOneDonorStartedTriggered)
            {
                var currentContext = matchingAlgorithmSearchTrackingContextManager.Retrieve();

                var matchingAlgorithmAttemptTimingEvent = new MatchingAlgorithmAttemptTimingEvent
                {
                    SearchRequestId = currentContext.SearchRequestId,
                    AttemptNumber = currentContext.AttemptNumber,
                    TimeUtc = DateTime.UtcNow
                };

                scoringOneDonorStartedTriggered = true;
                await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                    matchingAlgorithmAttemptTimingEvent, SearchTrackingEventType.MatchingAlgorithmCoreScoringStarted);
            }
        }

        public async Task ProcessCoreScoringAllDonorsEnded()
        {
            var currentContext = matchingAlgorithmSearchTrackingContextManager.Retrieve();

            var matchingAlgorithmAttemptTimingEvent = new MatchingAlgorithmAttemptTimingEvent
            {
                SearchRequestId = currentContext.SearchRequestId,
                AttemptNumber = currentContext.AttemptNumber,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchingAlgorithmAttemptTimingEvent, SearchTrackingEventType.MatchingAlgorithmCoreScoringEnded);
        }

        public async Task ProcessPersistingResultsStarted()
        {
            var currentContext = matchingAlgorithmSearchTrackingContextManager.Retrieve();

            var matchingAlgorithmAttemptTimingEvent = new MatchingAlgorithmAttemptTimingEvent
            {
                SearchRequestId = currentContext.SearchRequestId,
                AttemptNumber = currentContext.AttemptNumber,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchingAlgorithmAttemptTimingEvent, SearchTrackingEventType.MatchingAlgorithmPersistingResultsStarted);
        }

        public async Task ProcessPersistingResultsEnded()
        {
            var currentContext = matchingAlgorithmSearchTrackingContextManager.Retrieve();

            var matchingAlgorithmAttemptTimingEvent = new MatchingAlgorithmAttemptTimingEvent
            {
                SearchRequestId = currentContext.SearchRequestId,
                AttemptNumber = currentContext.AttemptNumber,
                TimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                matchingAlgorithmAttemptTimingEvent, SearchTrackingEventType.MatchingAlgorithmPersistingResultsEnded);
        }

        public async Task ProcessCompleted(MatchingAlgorithmCompletedEvent completedEvent)
        {
            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(completedEvent, SearchTrackingEventType.MatchingAlgorithmCompleted);
        }
    }
}