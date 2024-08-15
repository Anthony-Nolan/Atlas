using System;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.SearchTracking.Common.Clients;
using NSubstitute;
using NUnit.Framework;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Models;
using Atlas.SearchTracking.Common.Models;
using Atlas.SearchTracking.Common.Enums;
using FluentAssertions;

namespace Atlas.MatchingAlgorithm.Test.Services.Search
{
    [TestFixture]
    public class MatchingAlgorithmSearchTrackingDispatcherTests
    {
        private ISearchTrackingServiceBusClient searchTrackingServiceBusClient;
        private IMatchingAlgorithmSearchTrackingContextManager searchTrackingContextManager;

        private MatchingAlgorithmSearchTrackingDispatcher searchTrackingDispatcher;

        [SetUp]
        public void SetUp()
        {
            searchTrackingServiceBusClient = Substitute.For<ISearchTrackingServiceBusClient>();
            searchTrackingContextManager = Substitute.For<IMatchingAlgorithmSearchTrackingContextManager>();
            searchTrackingContextManager.Retrieve().ReturnsForAnyArgs(new MatchingAlgorithmSearchTrackingContext
                { SearchRequestId = new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"), AttemptNumber = 1 });

            searchTrackingDispatcher = new MatchingAlgorithmSearchTrackingDispatcher(searchTrackingContextManager, searchTrackingServiceBusClient);
        }

        [Test]
        public async Task DispatchSearchTrackingEvent_WhenMatchingAlgorithmStarted_DispatchesEventWithId()
        {
            const byte attemptNumber = 1;
            var initiationTime = new DateTime(2024, 8, 12);
            var startTime = new DateTime(2024, 8, 13);

            var expectedMatchingAlgorithmAttemptStartedEvent = new MatchingAlgorithmAttemptStartedEvent
            {
                SearchRequestId = new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                AttemptNumber = attemptNumber,
                InitiationTimeUtc = initiationTime,
                StartTimeUtc = startTime
            };

            MatchingAlgorithmAttemptStartedEvent actualAttemptStartedEvent = null;

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                Arg.Do<MatchingAlgorithmAttemptStartedEvent>(x => actualAttemptStartedEvent = x),
                Arg.Is(SearchTrackingEventType.MatchingAlgorithmAttemptStarted));

            await searchTrackingDispatcher.DispatchInitiationEvent(initiationTime, startTime);

            actualAttemptStartedEvent.Should().BeEquivalentTo(expectedMatchingAlgorithmAttemptStartedEvent);
        }

        [TestCase(SearchTrackingEventType.MatchingAlgorithmPersistingResultsEnded)]
        [TestCase(SearchTrackingEventType.MatchingAlgorithmPersistingResultsStarted)]
        [TestCase(SearchTrackingEventType.MatchingAlgorithmCoreScoringEnded)]
        [TestCase(SearchTrackingEventType.MatchingAlgorithmCoreScoringStarted)]
        [TestCase(SearchTrackingEventType.MatchingAlgorithmCoreMatchingEnded)]
        [TestCase(SearchTrackingEventType.MatchingAlgorithmCoreMatchingStarted)]
        [Test]
        public async Task DispatchMatchingAlgorithmAttemptTimingEvent_WhenTimingEventReceived_DispatchesEvent(SearchTrackingEventType eventType)
        {
            const byte attemptNumber = 1;
            var eventTime = new DateTime(2024, 8, 13);

            var expectedMatchingAlgorithmAttemptTimingEvent = new MatchingAlgorithmAttemptTimingEvent()
            {
                SearchRequestId = new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                AttemptNumber = attemptNumber,
                TimeUtc = eventTime
            };

            MatchingAlgorithmAttemptTimingEvent actualAttemptTimingEvent = null;

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                Arg.Do<MatchingAlgorithmAttemptTimingEvent>(x => actualAttemptTimingEvent = x),
                Arg.Is(eventType));

            await searchTrackingDispatcher.DispatchMatchingAlgorithmAttemptTimingEvent(eventType, eventTime);

            actualAttemptTimingEvent.Should().BeEquivalentTo(expectedMatchingAlgorithmAttemptTimingEvent);
        }
    }
}