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
                { SearchRequestId = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee", AttemptNumber = 1 });

            searchTrackingDispatcher = new MatchingAlgorithmSearchTrackingDispatcher(searchTrackingContextManager, searchTrackingServiceBusClient);
        }

        [Test]
        public async Task DispatchSearchTrackingEvent_WhenMatchingAlgorithmStarted_DispatchesEventWithId()
        {
            const byte attemptNumber = 1;
            DateTime initiationTime = new DateTime(2024, 8, 12);
            DateTime startTime = new DateTime(2024, 8, 13);

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
    }
}