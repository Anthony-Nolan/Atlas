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
    [TestFixture]
    public class MatchPredictionRepositoryTests : SearchTrackingContextTestBase
    {
        private IMatchPredictionRepository matchPredictionRepository;
        private SearchRequestMatchPredictionTiming defaultMatchPrediction;

        [SetUp]
        public async Task SetUp()
        {
            await SetUpBase();
            matchPredictionRepository = new MatchPredictionRepository(SearchTrackingContext);
            await InitiateData();
            defaultMatchPrediction = MatchPredictionEntityBuilder.Default.Build();
        }

        [TearDown]
        public async Task TearDown()
        {
            await TearDownBase();
        }

        [Test]
        public async Task GetMatchPrediction_WhenDoesNotExist_ThrowsException()
        {
            const int id = 2;

            var act = async () => await matchPredictionRepository.GetSearchRequestMatchPredictionById(id);

            act.Should().Throw<Exception>().WithMessage($"Match prediction timing for search id { id } not found");
        }

        [Test]
        public async Task TrackMatchPredictionStartedEvent_WithValidData_IsAddedToDb()
        {
            const int expectedSearchRequestId = 1;
            var expectedMatchPredictionEntity = defaultMatchPrediction;

            var matchPredictionStartedEvent = new MatchPredictionStartedEvent
            {
                SearchRequestId = expectedMatchPredictionEntity.SearchRequestId,
                InitiationTimeUtc = expectedMatchPredictionEntity.InitiationTimeUtc,
                StartTimeUtc = expectedMatchPredictionEntity.StartTimeUtc,
            };

            await matchPredictionRepository.TrackStartedEvent(matchPredictionStartedEvent);

            var actualMatchPredictionEntity = await matchPredictionRepository.GetSearchRequestMatchPredictionById(expectedSearchRequestId);

            expectedMatchPredictionEntity.Should().BeEquivalentTo(actualMatchPredictionEntity, options => options
                .Excluding(a => a.SearchRequest));
        }

        [TestCase(SearchTrackingEventType.MatchPredictionPersistingResultsEnded, nameof(SearchRequestMatchPredictionTiming.PersistingResults_EndTimeUtc))]
        [TestCase(SearchTrackingEventType.MatchPredictionPersistingResultsStarted, nameof(SearchRequestMatchPredictionTiming.PersistingResults_StartTimeUtc))]
        [TestCase(SearchTrackingEventType.MatchPredictionRunningBatchesEnded, nameof(SearchRequestMatchPredictionTiming.AlgorithmCore_RunningBatches_EndTimeUtc))]
        [TestCase(SearchTrackingEventType.MatchPredictionRunningBatchesStarted, nameof(SearchRequestMatchPredictionTiming.AlgorithmCore_RunningBatches_StartTimeUtc))]
        [TestCase(SearchTrackingEventType.MatchPredictionBatchPreparationEnded, nameof(SearchRequestMatchPredictionTiming.PrepareBatches_EndTimeUtc))]
        [TestCase(SearchTrackingEventType.MatchPredictionBatchPreparationStarted, nameof(SearchRequestMatchPredictionTiming.PrepareBatches_StartTimeUtc))]
        [Test]
        public async Task TrackMatchPredictionTimingEvent_WhenCompleted_IsSavedToDb(
            SearchTrackingEventType searchTrackingEventType, string dbColumnName)
        {
            await CreateDefaultMatchPrediction();

            const int expectedSearchRequestId = 1;
            var expectedMatchPredictionEntity = MatchPredictionEntityBuilder.UpdateTimings.Build();

            var matchPredictionTimingEvent = new MatchPredictionTimingEvent
            {
                SearchRequestId = expectedMatchPredictionEntity.SearchRequestId,
                TimeUtc = new DateTime(2022, 12, 31)
            };

            await matchPredictionRepository.TrackTimingEvent(matchPredictionTimingEvent, searchTrackingEventType);

            var actualMatchPredictionEntity = await matchPredictionRepository.GetSearchRequestMatchPredictionById(expectedSearchRequestId);

            expectedMatchPredictionEntity.GetType().GetProperty(dbColumnName).GetValue(expectedMatchPredictionEntity).Should()
                .BeEquivalentTo(actualMatchPredictionEntity.GetType().GetProperty(dbColumnName).GetValue(actualMatchPredictionEntity));
        }

        [Test]
        public async Task TrackMatchPredictionCompletedEvent_WhenCompleted_IsSavedToDb()
        {
            await CreateDefaultMatchPrediction();
            var expectedMatchPredictionEntity = MatchPredictionEntityBuilder.Completed.Build();

            var matchPredictionCompletedEvent = new MatchPredictionCompletedEvent
            {
                SearchRequestId = expectedMatchPredictionEntity.SearchRequestId,
                CompletionTimeUtc = expectedMatchPredictionEntity.CompletionTimeUtc.Value
            };

            await matchPredictionRepository.TrackCompletedEvent(matchPredictionCompletedEvent);

            var actualMatchPredictionEntity = await matchPredictionRepository.GetSearchRequestMatchPredictionById(expectedMatchPredictionEntity.SearchRequestId);

            expectedMatchPredictionEntity.Should().BeEquivalentTo(actualMatchPredictionEntity, options => options
                .Excluding(a => a.SearchRequest));
        }

        private async Task CreateDefaultMatchPrediction()
        {
            var matchPrediction = MatchPredictionEntityBuilder.Default.Build();
            await SearchTrackingContext.SearchRequestMatchPredictionTimings.AddAsync(matchPrediction);
            await SearchTrackingContext.SaveChangesAsync();
        }
    }
}
