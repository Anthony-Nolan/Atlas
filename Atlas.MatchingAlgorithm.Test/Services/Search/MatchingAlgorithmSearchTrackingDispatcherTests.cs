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
using FluentAssertions.Extensions;

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
                { SearchIdentifier = new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"), AttemptNumber = 1 });

            searchTrackingDispatcher = new MatchingAlgorithmSearchTrackingDispatcher(searchTrackingContextManager, searchTrackingServiceBusClient);
        }

        [Test]
        public async Task ProcessSearchTrackingEvent_WhenMatchingAlgorithmStarted_DispatchesEventWithId()
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

            await searchTrackingDispatcher.ProcessInitiation(initiationTime, startTime);

            actualAttemptStartedEvent.Should().BeEquivalentTo(expectedMatchingAlgorithmAttemptStartedEvent);
        }

        [Test]
        public async Task MatchingAlgorithmProcessCoreMatching_WhenStarted_DispatchesEvent()
        {
            const byte attemptNumber = 1;
            var eventTime = DateTime.UtcNow;

            var expectedMatchingAlgorithmAttemptTimingEvent = new MatchingAlgorithmAttemptTimingEvent()
            {
                SearchRequestId = new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                AttemptNumber = attemptNumber,
                TimeUtc = eventTime
            };

            MatchingAlgorithmAttemptTimingEvent actualAttemptTimingEvent = null;

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                Arg.Do<MatchingAlgorithmAttemptTimingEvent>(x => actualAttemptTimingEvent = x),
                Arg.Is(SearchTrackingEventType.MatchingAlgorithmCoreMatchingStarted));

            await searchTrackingDispatcher.ProcessCoreMatchingStarted();

            actualAttemptTimingEvent.Should().BeEquivalentTo(expectedMatchingAlgorithmAttemptTimingEvent, options =>
            {
                options.Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, 1.Seconds())).WhenTypeIs<DateTime>();
                return options;
            });
        }

        [Test]
        public async Task MatchingAlgorithmProcessCoreMatching_WhenEnded_DispatchesEvent()
        {
            const byte attemptNumber = 1;
            var eventTime = DateTime.UtcNow;

            var expectedMatchingAlgorithmAttemptTimingEvent = new MatchingAlgorithmAttemptTimingEvent()
            {
                SearchRequestId = new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                AttemptNumber = attemptNumber,
                TimeUtc = eventTime
            };

            MatchingAlgorithmAttemptTimingEvent actualAttemptTimingEvent = null;

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                Arg.Do<MatchingAlgorithmAttemptTimingEvent>(x => actualAttemptTimingEvent = x),
                Arg.Is(SearchTrackingEventType.MatchingAlgorithmCoreMatchingEnded));

            await searchTrackingDispatcher.ProcessCoreMatchingEnded();

            actualAttemptTimingEvent.Should().BeEquivalentTo(expectedMatchingAlgorithmAttemptTimingEvent, options =>
            {
                options.Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, 1.Seconds())).WhenTypeIs<DateTime>();
                return options;
            });
        }

        [Test]
        public async Task MatchingAlgorithmProcessCoreScoringOneDonor_WhenStarted_DispatchesEvent()
        {
            const byte attemptNumber = 1;
            var eventTime = DateTime.UtcNow;

            var expectedMatchingAlgorithmAttemptTimingEvent = new MatchingAlgorithmAttemptTimingEvent()
            {
                SearchRequestId = new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                AttemptNumber = attemptNumber,
                TimeUtc = eventTime
            };

            MatchingAlgorithmAttemptTimingEvent actualAttemptTimingEvent = null;

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                Arg.Do<MatchingAlgorithmAttemptTimingEvent>(x => actualAttemptTimingEvent = x),
                Arg.Is(SearchTrackingEventType.MatchingAlgorithmCoreScoringStarted));

            await searchTrackingDispatcher.ProcessCoreScoringOneDonorStarted();

            actualAttemptTimingEvent.Should().BeEquivalentTo(expectedMatchingAlgorithmAttemptTimingEvent, options =>
            {
                options.Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, 1.Seconds())).WhenTypeIs<DateTime>();
                return options;
            });
        }

        [Test]
        public async Task MatchingAlgorithmProcessCoreScoringAllDonors_WhenEnded_DispatchesEvent()
        {
            const byte attemptNumber = 1;
            var eventTime = DateTime.UtcNow;

            var expectedMatchingAlgorithmAttemptTimingEvent = new MatchingAlgorithmAttemptTimingEvent()
            {
                SearchRequestId = new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                AttemptNumber = attemptNumber,
                TimeUtc = eventTime
            };

            MatchingAlgorithmAttemptTimingEvent actualAttemptTimingEvent = null;

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                Arg.Do<MatchingAlgorithmAttemptTimingEvent>(x => actualAttemptTimingEvent = x),
                Arg.Is(SearchTrackingEventType.MatchingAlgorithmCoreScoringEnded));

            await searchTrackingDispatcher.ProcessCoreScoringAllDonorsEnded();

            actualAttemptTimingEvent.Should().BeEquivalentTo(expectedMatchingAlgorithmAttemptTimingEvent, options =>
            {
                options.Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, 1.Seconds())).WhenTypeIs<DateTime>();
                return options;
            });
        }

        [Test]
        public async Task MatchingAlgorithmProcessPersistingResults_WhenStarted_DispatchesEvent()
        {
            const byte attemptNumber = 1;
            var eventTime = DateTime.UtcNow;

            var expectedMatchingAlgorithmAttemptTimingEvent = new MatchingAlgorithmAttemptTimingEvent()
            {
                SearchRequestId = new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                AttemptNumber = attemptNumber,
                TimeUtc = eventTime
            };

            MatchingAlgorithmAttemptTimingEvent actualAttemptTimingEvent = null;

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                Arg.Do<MatchingAlgorithmAttemptTimingEvent>(x => actualAttemptTimingEvent = x),
                Arg.Is(SearchTrackingEventType.MatchingAlgorithmPersistingResultsStarted));

            await searchTrackingDispatcher.ProcessPersistingResultsStarted();

            actualAttemptTimingEvent.Should().BeEquivalentTo(expectedMatchingAlgorithmAttemptTimingEvent, options =>
            {
                options.Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, 1.Seconds())).WhenTypeIs<DateTime>();
                return options;
            });
        }

        [Test]
        public async Task MatchingAlgorithmProcessPersistingResults_WhenEnded_DispatchesEvent()
        {
            const byte attemptNumber = 1;
            var eventTime = DateTime.UtcNow;

            var expectedMatchingAlgorithmAttemptTimingEvent = new MatchingAlgorithmAttemptTimingEvent()
            {
                SearchRequestId = new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                AttemptNumber = attemptNumber,
                TimeUtc = eventTime
            };

            MatchingAlgorithmAttemptTimingEvent actualAttemptTimingEvent = null;

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                Arg.Do<MatchingAlgorithmAttemptTimingEvent>(x => actualAttemptTimingEvent = x),
                Arg.Is(SearchTrackingEventType.MatchingAlgorithmPersistingResultsEnded));

            await searchTrackingDispatcher.ProcessPersistingResultsEnded();

            actualAttemptTimingEvent.Should().BeEquivalentTo(expectedMatchingAlgorithmAttemptTimingEvent, options =>
            {
                options.Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, 1.Seconds())).WhenTypeIs<DateTime>();
                return options;
            });
        }
    }
}