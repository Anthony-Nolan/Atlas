using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Common.Models;
using Atlas.SearchTracking.Data.Models;
using Atlas.SearchTracking.Data.Repositories;
using Atlas.SearchTracking.Data.Test.Builders;
using Atlas.SearchTracking.Data.Test.TestHelpers;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.SearchTracking.Data.Test.Repositories
{
    public class SearchRequestMatchingAlgorithmAttemptsRepositoryTests : SearchTrackingContextTestBase
    {

        private ISearchRequestMatchingAlgorithmAttemptsRepository searchRequestMatchingAlgorithmAttemptsRepository;
        private SearchRequestMatchingAlgorithmAttempts defaultSearchRequestMatchingAlgorithmAttempts;

        [SetUp]
        public async Task SetUp()
        {
            await SetUpBase();
            searchRequestMatchingAlgorithmAttemptsRepository = new SearchRequestMatchingAlgorithmAttemptsRepository(SearchTrackingContext);
            await InitiateData();
            defaultSearchRequestMatchingAlgorithmAttempts = MatchingAlgorithmAttemptBuilder.Default.Build();
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

            var act = async () => await searchRequestMatchingAlgorithmAttemptsRepository
                .GetSearchRequestMatchingAlgorithmAttemptsById(id);

            act.Should().Throw<Exception>().WithMessage($"Matching algorithm attempt timing with id {id} not found");
        }

        [Test]
        public async Task TrackMatchingAlgorithmAttemptStartedEvent_WithValidData_IsAddedToDb()
        {
            const int expectedSearchRequestId = 1;
            var expectedSearchRequestGuid = new Guid("aaaaaaaa-bbbb-cccc-dddd-000000000000");

            var expectedSearchRequestMatchingAlgorithmAttemptsEntity = defaultSearchRequestMatchingAlgorithmAttempts;

            var matchingAlgorithmStartedEvent = new MatchingAlgorithmAttemptStartedEvent
            {
                SearchRequestId = expectedSearchRequestGuid,
                AttemptNumber = expectedSearchRequestMatchingAlgorithmAttemptsEntity.AttemptNumber,
                InitiationTimeUtc = expectedSearchRequestMatchingAlgorithmAttemptsEntity.InitiationTimeUtc,
                StartTimeUtc = expectedSearchRequestMatchingAlgorithmAttemptsEntity.StartTimeUtc,
            };

            await searchRequestMatchingAlgorithmAttemptsRepository.TrackStartedEvent(matchingAlgorithmStartedEvent);

            var actualSearchRequestMatchingAlgorithmAttemptsEntity = await searchRequestMatchingAlgorithmAttemptsRepository.GetSearchRequestMatchingAlgorithmAttemptsById(expectedSearchRequestId);

            expectedSearchRequestMatchingAlgorithmAttemptsEntity.Should().BeEquivalentTo(actualSearchRequestMatchingAlgorithmAttemptsEntity, options => options
                           .Excluding(a => a.SearchRequest));
        }


        [TestCase(SearchTrackingEventType.MatchingAlgorithmPersistingResultsEnded, nameof(SearchRequestMatchingAlgorithmAttempts.PersistingResults_EndTimeUtc))]
        [TestCase(SearchTrackingEventType.MatchingAlgorithmPersistingResultsStarted, nameof(SearchRequestMatchingAlgorithmAttempts.PersistingResults_StartTimeUtc))]
        [TestCase(SearchTrackingEventType.MatchingAlgorithmCoreScoringEnded, nameof(SearchRequestMatchingAlgorithmAttempts.AlgorithmCore_Scoring_EndTimeUtc))]
        [TestCase(SearchTrackingEventType.MatchingAlgorithmCoreScoringStarted, nameof(SearchRequestMatchingAlgorithmAttempts.AlgorithmCore_Scoring_StartTimeUtc))]
        [TestCase(SearchTrackingEventType.MatchingAlgorithmCoreMatchingEnded, nameof(SearchRequestMatchingAlgorithmAttempts.AlgorithmCore_Matching_EndTimeUtc))]
        [TestCase(SearchTrackingEventType.MatchingAlgorithmCoreMatchingStarted, nameof(SearchRequestMatchingAlgorithmAttempts.AlgorithmCore_Matching_StartTimeUtc))]
        [Test]
        public async Task TrackMatchingAlgorithmAttemptTimingEvent_WithValidData_IsAddedToDb(
            SearchTrackingEventType searchTrackingEventType, string dbColumnName)
        {
            const int expectedSearchRequestId = 1;
            var expectedSearchRequestGuid = new Guid("aaaaaaaa-bbbb-cccc-dddd-000000000000");

            await CreateDefaultMatchingAlgorithmAttempt();

            var expectedSearchRequestMatchingAlgorithmAttemptsEntity = MatchingAlgorithmAttemptBuilder.UpdateTimings.Build();

            var matchingAlgorithmAttemptTimingEvent = new MatchingAlgorithmAttemptTimingEvent
            {
                SearchRequestId = expectedSearchRequestGuid,
                AttemptNumber = expectedSearchRequestMatchingAlgorithmAttemptsEntity.AttemptNumber,
                TimeUtc = new DateTime(2022, 12, 31)
            };

            await searchRequestMatchingAlgorithmAttemptsRepository.TrackTimingEvent(matchingAlgorithmAttemptTimingEvent, searchTrackingEventType);

            var actualSearchRequestMatchingAlgorithmAttemptsEntity = await searchRequestMatchingAlgorithmAttemptsRepository
                .GetSearchRequestMatchingAlgorithmAttemptsById(expectedSearchRequestId);

            expectedSearchRequestMatchingAlgorithmAttemptsEntity.GetType().GetProperty(dbColumnName).GetValue(
                    expectedSearchRequestMatchingAlgorithmAttemptsEntity).Should()
                .BeEquivalentTo(actualSearchRequestMatchingAlgorithmAttemptsEntity.GetType().GetProperty(dbColumnName).GetValue(
                    actualSearchRequestMatchingAlgorithmAttemptsEntity));
        }

        [Test]
        public async Task TrackMatchingAlgorithmAttemptCompletedEvent_WhenCompleted_IsSavedToDb()
        {
            const int expectedSearchRequestId = 1;
            var expectedSearchRequestGuid = new Guid("aaaaaaaa-bbbb-cccc-dddd-000000000000");
            await CreateDefaultMatchingAlgorithmAttempt();

            var expectedSearchRequestMatchingAlgorithmAttemptsEntity = MatchingAlgorithmAttemptBuilder.Completed.Build();

            var matchingAlgorithmCompletedEvent = new MatchingAlgorithmCompletedEvent
            {
                SearchRequestId = expectedSearchRequestGuid,
                AttemptNumber = expectedSearchRequestMatchingAlgorithmAttemptsEntity.AttemptNumber,
                CompletionTimeUtc = expectedSearchRequestMatchingAlgorithmAttemptsEntity.CompletionTimeUtc.Value
            };

            await searchRequestMatchingAlgorithmAttemptsRepository.TrackCompletedEvent(matchingAlgorithmCompletedEvent);

            var actualSearchRequestMatchingAlgorithmAttemptsEntity = await searchRequestMatchingAlgorithmAttemptsRepository.
                GetSearchRequestMatchingAlgorithmAttemptsById(expectedSearchRequestId);

            expectedSearchRequestMatchingAlgorithmAttemptsEntity.Should().BeEquivalentTo(
                actualSearchRequestMatchingAlgorithmAttemptsEntity, options => options.Excluding(a => a.SearchRequest));
        }

        private async Task CreateDefaultMatchingAlgorithmAttempt()
        {
            var matchingAlgorithmAttempt = MatchingAlgorithmAttemptBuilder.Default.Build();
            await SearchTrackingContext.SearchRequestMatchingAlgorithmAttempts.AddAsync(matchingAlgorithmAttempt);
            await SearchTrackingContext.SaveChangesAsync();
        }
    }
}
