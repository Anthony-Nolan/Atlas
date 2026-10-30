using System;
using System.Collections.Generic;
using Atlas.Common.ApplicationInsights;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.SearchTracking.Common.Clients;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using System.Threading.Tasks;
using Atlas.SearchTracking.Common.Models;
using Atlas.SearchTracking.Common.Enums;
using AutoFixture;
using AwesomeAssertions;
using AwesomeAssertions.Extensions;

namespace Atlas.MatchingAlgorithm.Test.Services.Search
{
    [TestFixture]
    public class SearchTrackingEventPublisherTests
    {
        private ISearchTrackingServiceBusClient searchTrackingServiceBusClient;
        private IAtlasLogger logger;
        private Fixture fixture;
        private MatchingAlgorithmSearchTrackingContext context;

        private SearchTrackingEventPublisher searchTrackingDispatcher;

        [SetUp]
        public void SetUp()
        {
            searchTrackingServiceBusClient = Substitute.For<ISearchTrackingServiceBusClient>();
            logger = Substitute.For<IAtlasLogger>();
            fixture = new Fixture();
            context = new MatchingAlgorithmSearchTrackingContext
                { SearchIdentifier = new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"), AttemptNumber = 1 };

            searchTrackingDispatcher = new SearchTrackingEventPublisher(context, searchTrackingServiceBusClient, logger);
        }

        [Test]
        public async Task ProcessSearchTrackingEvent_WhenMatchingAlgorithmStarted_DispatchesEventWithId()
        {
            const byte attemptNumber = 1;
            var initiationTime = new DateTime(2024, 8, 12);
            var startTime = new DateTime(2024, 8, 13);

            var expectedMatchingAlgorithmAttemptStartedEvent = new MatchingAlgorithmAttemptStartedEvent
            {
                SearchIdentifier = new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
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
                SearchIdentifier = new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
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
                SearchIdentifier = new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
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
                SearchIdentifier = new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
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
                SearchIdentifier = new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
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
                SearchIdentifier = new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
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
                SearchIdentifier = new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
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

        private static IEnumerable<TestCaseData> StepsBeforeCompletion()
        {
            yield return Step(p => p.ProcessInitiation(DateTime.UtcNow, DateTime.UtcNow), SearchTrackingEventType.MatchingAlgorithmAttemptStarted);
            yield return Step(p => p.ProcessCoreMatchingStarted(), SearchTrackingEventType.MatchingAlgorithmCoreMatchingStarted);
            yield return Step(p => p.ProcessCoreMatchingEnded(), SearchTrackingEventType.MatchingAlgorithmCoreMatchingEnded);
            yield return Step(p => p.ProcessCoreScoringOneDonorStarted(), SearchTrackingEventType.MatchingAlgorithmCoreScoringStarted);
            yield return Step(p => p.ProcessCoreScoringAllDonorsEnded(), SearchTrackingEventType.MatchingAlgorithmCoreScoringEnded);
            yield return Step(p => p.ProcessPersistingResultsStarted(), SearchTrackingEventType.MatchingAlgorithmPersistingResultsStarted);
            yield return Step(p => p.ProcessPersistingResultsEnded(), SearchTrackingEventType.MatchingAlgorithmPersistingResultsEnded);

            static TestCaseData Step(Func<SearchTrackingEventPublisher, Task> process, SearchTrackingEventType eventType) =>
                new TestCaseData(process, eventType).SetArgDisplayNames(eventType.ToString());
        }

        [TestCaseSource(nameof(StepsBeforeCompletion))]
        public async Task ProcessStep_SendsItsEventToApplicationInsights(Func<SearchTrackingEventPublisher, Task> process, SearchTrackingEventType eventType)
        {
            Dictionary<string, string> sentProperties = null;
            logger.When(l => l.SendEvent(
                    eventType.ToString(),
                    LogLevel.Info,
                    Arg.Any<Dictionary<string, string>>(),
                    Arg.Any<Dictionary<string, double>>()))
                .Do(call => sentProperties = call.ArgAt<Dictionary<string, string>>(2));

            await process(searchTrackingDispatcher);

            sentProperties.Should().Contain(nameof(ISearchTrackingEvent.SearchIdentifier), context.SearchIdentifier.ToString())
                .And.Contain(nameof(ISearchTrackingMatchingAttemptEvent.AttemptNumber), context.AttemptNumber.ToString());
        }

        [Test]
        public async Task ProcessCompleted_WhenAttemptFailed_SendsFailureTypeWithoutMessageOrStacktrace()
        {
            var failureInfo = fixture.Create<MatchingAlgorithmFailureInfo>();

            Dictionary<string, string> sentProperties = null;
            logger.When(l => l.SendEvent(
                    nameof(SearchTrackingEventType.MatchingAlgorithmCompleted),
                    LogLevel.Info,
                    Arg.Any<Dictionary<string, string>>(),
                    Arg.Any<Dictionary<string, double>>()))
                .Do(call => sentProperties = call.ArgAt<Dictionary<string, string>>(2));

            await searchTrackingDispatcher.ProcessCompleted((
                HlaNomenclatureVersion: fixture.Create<string>(),
                ResultsSentTimeUtc: fixture.Create<DateTime>(),
                NumberOfResults: null,
                FailureInfo: failureInfo,
                RepeatSearchResultsDetails: null,
                NumberOfMatching: null));

            sentProperties.Should().Contain("CompletionDetails.IsSuccessful", "false")
                .And.Contain("CompletionDetails.FailureInfo.Type", failureInfo.Type.ToString())
                .And.NotContainKeys("CompletionDetails.FailureInfo.Message", "CompletionDetails.FailureInfo.ExceptionStacktrace");
        }

        [Test]
        public async Task ProcessCoreScoringOneDonorStarted_WhenCalledTwice_SendsOneEvent()
        {
            await searchTrackingDispatcher.ProcessCoreScoringOneDonorStarted();
            await searchTrackingDispatcher.ProcessCoreScoringOneDonorStarted();

            logger.Received(1).SendEvent(
                nameof(SearchTrackingEventType.MatchingAlgorithmCoreScoringStarted),
                LogLevel.Info,
                Arg.Any<Dictionary<string, string>>(),
                Arg.Any<Dictionary<string, double>>());
        }

        [Test]
        public async Task ProcessCoreMatchingStarted_WhenServiceBusPublishFails_StillSendsEventToApplicationInsights()
        {
            searchTrackingServiceBusClient
                .PublishSearchTrackingEvent(Arg.Any<MatchingAlgorithmAttemptTimingEvent>(), Arg.Any<SearchTrackingEventType>())
                .ThrowsAsync(fixture.Create<Exception>());

            var act = () => searchTrackingDispatcher.ProcessCoreMatchingStarted();

            await act.Should().ThrowAsync<Exception>();
            logger.Received(1).SendEvent(
                nameof(SearchTrackingEventType.MatchingAlgorithmCoreMatchingStarted),
                LogLevel.Info,
                Arg.Any<Dictionary<string, string>>(),
                Arg.Any<Dictionary<string, double>>());
        }
    }
}