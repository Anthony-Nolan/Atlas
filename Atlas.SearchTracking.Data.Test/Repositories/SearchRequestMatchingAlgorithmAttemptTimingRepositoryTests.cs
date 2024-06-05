using Atlas.SearchTracking.Data.Models;
using Atlas.SearchTracking.Data.Repositories;
using Atlas.SearchTracking.Data.Test.Builders;
using Atlas.SearchTracking.Data.Test.TestHelpers;
using Atlas.SearchTracking.Enums;
using Atlas.SearchTracking.Models;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.SearchTracking.Data.Test.Repositories
{
    public class SearchRequestMatchingAlgorithmAttemptTimingRepositoryTests : SearchTrackingContextTestBase
    {

        private ISearchRequestMatchingAlgorithmAttemptTimingRepository searchRequestMatchingAlgorithmAttemptTimingRepository;
        private SearchRequestMatchingAlgorithmAttemptTiming defaultSearchRequestMatchingAlgorithmAttemptTiming;

        [SetUp]
        public async Task SetUp()
        {
            await SetUpBase();
            searchRequestMatchingAlgorithmAttemptTimingRepository = new SearchRequestMatchingAlgorithmAttemptTimingRepository(SearchTrackingContext);
            await InitiateData();
            defaultSearchRequestMatchingAlgorithmAttemptTiming = MatchingAlgorithmAttemptBuilder.Default.Build();
        }

        [TearDown]
        public async Task TearDown()
        {
            await TearDownBase();
        }

        [Test]
        public async Task GetMatchingAlgorithmAttempt_WhenRecordNotInDb_ThrowsException()
        {
            const int id = 2;

            var act = async () => await searchRequestMatchingAlgorithmAttemptTimingRepository
                .GetSearchRequestMatchingAlgorithmAttemptTimingById(id);

            act.Should().Throw<Exception>().WithMessage($"Matching algorithm attempt timing with id {id} not found");
        }

        [Test]
        public async Task TrackMatchingAlgorithmAttemptStartedEvent_WithValidData_IsAddedToDb()
        {
            const int expectedSearchRequestId = 1;

            var expectedSearchRequestMatchingAlgorithmAttemptTimingEntity = defaultSearchRequestMatchingAlgorithmAttemptTiming;

            var matchingAlgorithmStartedEvent = new MatchingAlgorithmAttemptStartedEvent
            {
                SearchRequestId = expectedSearchRequestMatchingAlgorithmAttemptTimingEntity.SearchRequestId,
                AttemptNumber = expectedSearchRequestMatchingAlgorithmAttemptTimingEntity.AttemptNumber,
                InitiationTimeUtc = expectedSearchRequestMatchingAlgorithmAttemptTimingEntity.InitiationTimeUtc,
                StartTimeUtc = expectedSearchRequestMatchingAlgorithmAttemptTimingEntity.StartTimeUtc,
            };

            await searchRequestMatchingAlgorithmAttemptTimingRepository.TrackStartedEvent(matchingAlgorithmStartedEvent);

            var actualSearchRequestMatchingAlgorithmAttemptTimingEntity = await searchRequestMatchingAlgorithmAttemptTimingRepository.GetSearchRequestMatchingAlgorithmAttemptTimingById(expectedSearchRequestId);

            expectedSearchRequestMatchingAlgorithmAttemptTimingEntity.Should().BeEquivalentTo(actualSearchRequestMatchingAlgorithmAttemptTimingEntity, options => options
                           .Excluding(a => a.SearchRequest));
        }


        [TestCase(SearchTrackingEventType.MatchingAlgorithmPersistingResultsEnded, nameof(SearchRequestMatchingAlgorithmAttemptTiming.PersistingResults_EndTimeUtc))]
        [TestCase(SearchTrackingEventType.MatchingAlgorithmPersistingResultsStarted, nameof(SearchRequestMatchingAlgorithmAttemptTiming.PersistingResults_StartTimeUtc))]
        [TestCase(SearchTrackingEventType.MatchingAlgorithmCoreScoringEnded, nameof(SearchRequestMatchingAlgorithmAttemptTiming.AlgorithmCore_Scoring_EndTimeUtc))]
        [TestCase(SearchTrackingEventType.MatchingAlgorithmCoreScoringStarted, nameof(SearchRequestMatchingAlgorithmAttemptTiming.AlgorithmCore_Scoring_StartTimeUtc))]
        [TestCase(SearchTrackingEventType.MatchingAlgorithmCoreMatchingEnded, nameof(SearchRequestMatchingAlgorithmAttemptTiming.AlgorithmCore_Matching_EndTimeUtc))]
        [TestCase(SearchTrackingEventType.MatchingAlgorithmCoreMatchingStarted, nameof(SearchRequestMatchingAlgorithmAttemptTiming.AlgorithmCore_Matching_StartTimeUtc))]
        [Test]
        public async Task TrackMatchingAlgorithmAttemptTimingEvent_WithValidData_IsAddedToDb(
            SearchTrackingEventType searchTrackingEventType, string dbColumnName)
        {
            const int expectedSearchRequestId = 1;
            await CreateDefaultMatchingAlgorithmAttempt();

            var expectedSearchRequestMatchingAlgorithmAttemptTimingEntity = MatchingAlgorithmAttemptBuilder.UpdateTimings.Build();

            var matchingAlgorithmAttemptTimingEvent = new MatchingAlgorithmAttemptTimingEvent
            {
                SearchRequestId = expectedSearchRequestMatchingAlgorithmAttemptTimingEntity.SearchRequestId,
                AttemptNumber = expectedSearchRequestMatchingAlgorithmAttemptTimingEntity.AttemptNumber,
                TimeUtc = new DateTime(2022, 12, 31)
            };

            await searchRequestMatchingAlgorithmAttemptTimingRepository.TrackTimingEvent(matchingAlgorithmAttemptTimingEvent, searchTrackingEventType);

            var actualSearchRequestMatchingAlgorithmAttemptTimingEntity = await searchRequestMatchingAlgorithmAttemptTimingRepository
                .GetSearchRequestMatchingAlgorithmAttemptTimingById(expectedSearchRequestId);

            expectedSearchRequestMatchingAlgorithmAttemptTimingEntity.GetType().GetProperty(dbColumnName).GetValue(
                    expectedSearchRequestMatchingAlgorithmAttemptTimingEntity).Should()
                .BeEquivalentTo(actualSearchRequestMatchingAlgorithmAttemptTimingEntity.GetType().GetProperty(dbColumnName).GetValue(
                    actualSearchRequestMatchingAlgorithmAttemptTimingEntity));
        }

        [Test]
        public async Task TrackMatchingAlgorithmAttemptCompletedEvent_WhenCompleted_IsSavedToDb()
        {
            const int expectedSearchRequestId = 1;
            await CreateDefaultMatchingAlgorithmAttempt();

            var expectedSearchRequestMatchingAlgorithmAttemptTimingEntity = MatchingAlgorithmAttemptBuilder.Completed.Build();

            var matchingAlgorithmCompletedEvent = new MatchingAlgorithmCompletedEvent
            {
                SearchRequestId = expectedSearchRequestMatchingAlgorithmAttemptTimingEntity.SearchRequestId,
                AttemptNumber = expectedSearchRequestMatchingAlgorithmAttemptTimingEntity.AttemptNumber,
                CompletionTimeUtc = expectedSearchRequestMatchingAlgorithmAttemptTimingEntity.CompletionTimeUtc.Value
            };

            await searchRequestMatchingAlgorithmAttemptTimingRepository.TrackCompletedEvent(matchingAlgorithmCompletedEvent);

            var actualSearchRequestMatchingAlgorithmAttemptTimingEntity = await searchRequestMatchingAlgorithmAttemptTimingRepository.
                GetSearchRequestMatchingAlgorithmAttemptTimingById(expectedSearchRequestId);

            expectedSearchRequestMatchingAlgorithmAttemptTimingEntity.Should().BeEquivalentTo(
                actualSearchRequestMatchingAlgorithmAttemptTimingEntity, options => options.Excluding(a => a.SearchRequest));
        }

        private async Task CreateDefaultMatchingAlgorithmAttempt()
        {
            var matchingAlgorithmAttempt = MatchingAlgorithmAttemptBuilder.Default.Build();
            await SearchTrackingContext.SearchRequestMatchingAlgorithmAttemptTimings.AddAsync(matchingAlgorithmAttempt);
            await SearchTrackingContext.SaveChangesAsync();
        }
    }
}
