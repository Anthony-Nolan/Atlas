using Atlas.SearchTracking.Common.Models;
using Atlas.SearchTracking.Data.Repositories;
using Atlas.SearchTracking.Services;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using Atlas.SearchTracking.Common.Enums;
using FluentAssertions;

namespace Atlas.SearchTracking.Test.Services
{
    [TestFixture]
    public class SearchTrackingEventProcessorTests
    {
        private ISearchRequestRepository searchRequestRepository;
        private ISearchRequestMatchingAlgorithmAttemptsRepository searchRequestMatchingAlgorithmAttemptsRepository;
        private IMatchPredictionRepository matchPredictionRepository;

        private ISearchTrackingEventProcessor searchTrackingEventProcessor;

        [SetUp]
        public void Setup()
        {
            searchRequestRepository = Substitute.For<ISearchRequestRepository>();
            searchRequestMatchingAlgorithmAttemptsRepository = Substitute.For<ISearchRequestMatchingAlgorithmAttemptsRepository>();
            matchPredictionRepository = Substitute.For<IMatchPredictionRepository>();

            searchTrackingEventProcessor = new SearchTrackingEventProcessor(
                searchRequestRepository,
                matchPredictionRepository,
                searchRequestMatchingAlgorithmAttemptsRepository);
        }

        [Test]
        public async Task SearchTrackingEventProcessor_WhenSearchRequested_UpdatesRepository()
        {
            SearchRequestedEvent actualSearchRequestedEvent = null;

            var expectedSearchRequestedEvent = new SearchRequestedEvent
            {
                SearchIdentifier = new Guid("aaaaaaaa-bbbb-cccc-dddd-000000000000"),
                IsRepeatSearch = false,
                OriginalSearchIdentifier = null,
                RepeatSearchCutOffDate = null,
                RequestJson = "RequestJson",
                SearchCriteria = "10/10",
                DonorType = "Adult",
                RequestTimeUtc = new DateTime(2024, 10, 1, 14, 30, 00),
                IsMatchPredictionRun = false,
                AreBetterMatchesIncluded = true,
                DonorRegistryCodes = ["ANDonor", "BNDonor"]
            };

            var body = JsonConvert.SerializeObject(expectedSearchRequestedEvent);
            var eventType = SearchTrackingEventType.SearchRequested;

            await searchRequestRepository.TrackSearchRequestedEvent(Arg.Do<SearchRequestedEvent>(x => actualSearchRequestedEvent = x));
            await searchTrackingEventProcessor.HandleEvent(body, eventType);

            actualSearchRequestedEvent.Should().BeEquivalentTo(expectedSearchRequestedEvent);
            await searchRequestRepository.Received(1).TrackSearchRequestedEvent(Arg.Any<SearchRequestedEvent>());
        }

        [Test]
        public async Task SearchTrackingEventProcessor_WhenMatchingAlgorithmStarted_UpdatesRepository()
        {
            MatchingAlgorithmAttemptStartedEvent actualMatchingAlgorithmAttemptStartedEvent = null;

            var expectedMatchingAlgorithmAttemptStartedEvent = new MatchingAlgorithmAttemptStartedEvent()
            {
                SearchIdentifier = new Guid("aaaaaaaa-bbbb-cccc-dddd-000000000000"),
                AttemptNumber = 0,
                InitiationTimeUtc = new DateTime(2024, 10, 24, 15, 0, 0),
                StartTimeUtc = new DateTime(2024, 10, 24, 15, 0, 2)
            };

            var body = JsonConvert.SerializeObject(expectedMatchingAlgorithmAttemptStartedEvent);
            var eventType = SearchTrackingEventType.MatchingAlgorithmAttemptStarted;

            await searchRequestMatchingAlgorithmAttemptsRepository.TrackStartedEvent(
                Arg.Do<MatchingAlgorithmAttemptStartedEvent>(x => actualMatchingAlgorithmAttemptStartedEvent = x));
            await searchTrackingEventProcessor.HandleEvent(body, eventType);

            actualMatchingAlgorithmAttemptStartedEvent.Should().BeEquivalentTo(expectedMatchingAlgorithmAttemptStartedEvent);
            await searchRequestMatchingAlgorithmAttemptsRepository.Received(1)
                .TrackStartedEvent(Arg.Any<MatchingAlgorithmAttemptStartedEvent>());
        }

        [TestCase(SearchTrackingEventType.MatchingAlgorithmPersistingResultsEnded)]
        [TestCase(SearchTrackingEventType.MatchingAlgorithmPersistingResultsStarted)]
        [TestCase(SearchTrackingEventType.MatchingAlgorithmCoreScoringEnded)]
        [TestCase(SearchTrackingEventType.MatchingAlgorithmCoreScoringStarted)]
        [TestCase(SearchTrackingEventType.MatchingAlgorithmCoreMatchingEnded)]
        [TestCase(SearchTrackingEventType.MatchingAlgorithmCoreMatchingStarted)]
        [Test]
        public async Task SearchTrackingEventProcessor_WhenTimingEventReceived_UpdatesMatchingAlgorithmRepository(SearchTrackingEventType eventType)
        {
            MatchingAlgorithmAttemptTimingEvent actualMatchingAlgorithmAttemptTimingEvent = null;

            var expectedMatchingAlgorithmAttemptTimingEvent = new MatchingAlgorithmAttemptTimingEvent
            {
                SearchIdentifier = new Guid("aaaaaaaa-bbbb-cccc-dddd-000000000000"),
                AttemptNumber = 0,
                TimeUtc = new DateTime(2024, 10, 24, 15, 0, 10)
            };

            var body = JsonConvert.SerializeObject(expectedMatchingAlgorithmAttemptTimingEvent);

            await searchRequestMatchingAlgorithmAttemptsRepository.TrackTimingEvent(
                Arg.Do<MatchingAlgorithmAttemptTimingEvent>(x => actualMatchingAlgorithmAttemptTimingEvent = x), eventType);
            await searchTrackingEventProcessor.HandleEvent(body, eventType);

            actualMatchingAlgorithmAttemptTimingEvent.Should().BeEquivalentTo(expectedMatchingAlgorithmAttemptTimingEvent);
            await searchRequestMatchingAlgorithmAttemptsRepository.Received(1).TrackTimingEvent(
                Arg.Any<MatchingAlgorithmAttemptTimingEvent>(), eventType);
        }

        [Test]
        public async Task SearchTrackingEventProcessor_WhenMatchingAlgorithmCompleted_UpdatesSearchRequestAndMatchingAlgorithmRepository()
        {
            MatchingAlgorithmCompletedEvent actualMatchingAlgorithmCompletedEvent = null;

            var body = JsonConvert.SerializeObject(new
            {
                SearchIdentifier = new Guid("aaaaaaaa-bbbb-cccc-dddd-000000000000"),
                AttemptNumber = 0,
                CompletionDetails = new
                {
                    IsSuccessful = true,
                    NumberOfMatching = 100,
                    NumberOfResults = 150
                },
                HlaNomenclatureVersion = "3.44.0",
                ResultsSent = true,
                ResultsSentTimeUtc = new DateTime(2024, 10, 24, 16, 0, 0)
            });

            var eventType = SearchTrackingEventType.MatchingAlgorithmCompleted;

            await searchRequestMatchingAlgorithmAttemptsRepository.TrackCompletedEvent(
                Arg.Do<MatchingAlgorithmCompletedEvent>(x => actualMatchingAlgorithmCompletedEvent = x));
            await searchTrackingEventProcessor.HandleEvent(body, eventType);

            var expectedMatchingAlgorithmCompletedEvent = new MatchingAlgorithmCompletedEvent()
            {
                SearchIdentifier = new Guid("aaaaaaaa-bbbb-cccc-dddd-000000000000"),
                AttemptNumber = 0,
                CompletionDetails = new MatchingAlgorithmCompletionDetails
                {
                    IsSuccessful = true,
                    NumberOfResults = 150
                },
                HlaNomenclatureVersion = "3.44.0",
                ResultsSent = true,
                ResultsSentTimeUtc = new DateTime(2024, 10, 24, 16, 0, 0)
            };

            actualMatchingAlgorithmCompletedEvent.Should().BeEquivalentTo(expectedMatchingAlgorithmCompletedEvent);
            await searchRequestMatchingAlgorithmAttemptsRepository.Received(1).TrackCompletedEvent(Arg.Any<MatchingAlgorithmCompletedEvent>());

            await searchRequestRepository.Received(1).TrackMatchingAlgorithmCompletedEvent(Arg.Any<MatchingAlgorithmCompletedEvent>());
        }

        [Test]
        public async Task SearchTrackingEventProcessor_WhenMatchPredictionStarted_UpdatesRepository()
        {
            MatchPredictionStartedEvent actualMatchPredictionStartedEvent = null;

            var expectedMatchPredictionStartedEvent = new MatchPredictionStartedEvent()
            {
                SearchIdentifier = new Guid("aaaaaaaa-bbbb-cccc-dddd-000000000000"),
                InitiationTimeUtc = new DateTime(2024, 10, 24, 15, 0, 30),
                StartTimeUtc = new DateTime(2024, 10, 24, 15, 0, 31)
            };

            var body = JsonConvert.SerializeObject(expectedMatchPredictionStartedEvent);
            var eventType = SearchTrackingEventType.MatchPredictionStarted;

            await matchPredictionRepository.TrackStartedEvent(
                Arg.Do<MatchPredictionStartedEvent>(x => actualMatchPredictionStartedEvent = x));
            await searchTrackingEventProcessor.HandleEvent(body, eventType);

            actualMatchPredictionStartedEvent.Should().BeEquivalentTo(expectedMatchPredictionStartedEvent);
            await matchPredictionRepository.Received(1).TrackStartedEvent(Arg.Any<MatchPredictionStartedEvent>());
        }


        [TestCase(SearchTrackingEventType.MatchPredictionBatchPreparationEnded)]
        [TestCase(SearchTrackingEventType.MatchPredictionBatchPreparationStarted)]
        [TestCase(SearchTrackingEventType.MatchPredictionRunningBatchesEnded)]
        [TestCase(SearchTrackingEventType.MatchPredictionRunningBatchesStarted)]
        [TestCase(SearchTrackingEventType.MatchPredictionPersistingResultsEnded)]
        [TestCase(SearchTrackingEventType.MatchPredictionPersistingResultsStarted)]
        [Test]
        public async Task SearchTrackingEventProcessor_WhenTimingEventReceived_UpdatesMatchPredictionRepository(SearchTrackingEventType eventType)
        {
            MatchPredictionTimingEvent actualMatchPredictionTimingEvent = null;

            var expectedMatchPredictionTimingEvent = new MatchPredictionTimingEvent
            {
                SearchIdentifier = new Guid("aaaaaaaa-bbbb-cccc-dddd-000000000000"),
                TimeUtc = new DateTime(2024, 10, 24, 15, 0, 0)
            };

            var body = JsonConvert.SerializeObject(expectedMatchPredictionTimingEvent);

            await matchPredictionRepository.TrackTimingEvent(
                Arg.Do<MatchPredictionTimingEvent>(x => actualMatchPredictionTimingEvent = x), eventType);
            await searchTrackingEventProcessor.HandleEvent(body, eventType);

            actualMatchPredictionTimingEvent.Should().BeEquivalentTo(expectedMatchPredictionTimingEvent);
            await matchPredictionRepository.Received(1).TrackTimingEvent(Arg.Any<MatchPredictionTimingEvent>(), eventType);
        }

        [Test]
        public async Task SearchTrackingEventProcessor_WhenMatchPredictionCompleted_UpdatesSearchRequestAndMatchPredictionRepository()
        {
            MatchPredictionCompletedEvent actualMatchPredictionCompletedEvent = null;

            var expectedMatchPredictionCompletedEvent = new MatchPredictionCompletedEvent()
            {
                SearchIdentifier = new Guid("aaaaaaaa-bbbb-cccc-dddd-000000000000"),
                CompletionTimeUtc = new DateTime(2024, 10, 24, 16, 0, 0),
                CompletionDetails = new MatchPredictionCompletionDetails
                {
                    FailureInfo = new MatchPredictionFailureInfo
                    {
                        Message = ""
                    },
                    IsSuccessful = true,
                    DonorsPerBatch = 100,
                    TotalNumberOfBatches = 6,
                }
            };

            var body = JsonConvert.SerializeObject(expectedMatchPredictionCompletedEvent);
            var eventType = SearchTrackingEventType.MatchPredictionCompleted;

            await matchPredictionRepository.TrackCompletedEvent(
                Arg.Do<MatchPredictionCompletedEvent>(x => actualMatchPredictionCompletedEvent = x));
            await searchTrackingEventProcessor.HandleEvent(body, eventType);

            actualMatchPredictionCompletedEvent.Should().BeEquivalentTo(expectedMatchPredictionCompletedEvent);
            await matchPredictionRepository.Received(1).TrackCompletedEvent(Arg.Any<MatchPredictionCompletedEvent>());

            await searchRequestRepository.Received(1).TrackMatchPredictionCompletedEvent(Arg.Any<MatchPredictionCompletedEvent>());
        }
    }
}
