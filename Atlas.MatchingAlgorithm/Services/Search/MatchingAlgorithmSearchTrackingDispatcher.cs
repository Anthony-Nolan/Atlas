using System;
using System.Threading.Tasks;
using Atlas.SearchTracking.Common.Clients;
using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Common.Models;

namespace Atlas.MatchingAlgorithm.Services.Search
{
    public interface IMatchingAlgorithmSearchTrackingDispatcher
    {
        Task DispatchInitiationEvent(DateTime initiationTime, DateTime startTime);

        Task DispatchMatchingAlgorithmAttemptTimingEvent(SearchTrackingEventType eventType, DateTime timing);
    }

    public class MatchingAlgorithmSearchTrackingDispatcher(
        IMatchingAlgorithmSearchTrackingContextManager matchingAlgorithmSearchTrackingContextManager,
        ISearchTrackingServiceBusClient searchTrackingServiceBusClient)
        : IMatchingAlgorithmSearchTrackingDispatcher
    {
        public async Task DispatchInitiationEvent(DateTime initiationTime, DateTime startTime)
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

        public async Task DispatchMatchingAlgorithmAttemptTimingEvent(SearchTrackingEventType eventType, DateTime timing)
        {
            var currentContext = matchingAlgorithmSearchTrackingContextManager.Retrieve();

            var matchingAlgorithmAttemptTimingEvent = new MatchingAlgorithmAttemptTimingEvent
            {
                SearchRequestId = currentContext.SearchRequestId,
                AttemptNumber = currentContext.AttemptNumber,
                TimeUtc = timing
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(matchingAlgorithmAttemptTimingEvent, eventType);
        }
    }
}