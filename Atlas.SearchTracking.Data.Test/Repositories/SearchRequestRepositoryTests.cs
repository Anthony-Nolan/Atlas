using Atlas.SearchTracking.Common.Models;
using Atlas.SearchTracking.Data.Repositories;
using Atlas.SearchTracking.Data.Test.Builders;
using Atlas.SearchTracking.Data.Test.TestHelpers;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.SearchTracking.Data.Test.Repositories
{
    [TestFixture]
    public class SearchRequestRepositoryTests : SearchTrackingContextTestBase
    {
        private ISearchRequestRepository searchRequestRepository;

        [SetUp]
        public async Task SetUp()
        {
            await SetUpBase();
            searchRequestRepository = new SearchRequestRepository(SearchTrackingContext);
            await InitiateData();
        }

        [TearDown]
        public async Task TearDown()
        {
            await TearDownBase();
        }

        [Test]
        public async Task GetSearchRequest_WhenRecordNotInDb_ThrowsException()
        {
            const int expectedSearchRequestId = 100;

            var act = async () => await searchRequestRepository.GetSearchRequestById(expectedSearchRequestId);

            act.Should().Throw<Exception>().WithMessage($"Search request with id {expectedSearchRequestId} not found");
        }

        [Test]
        public async Task TrackSearchRequestedEvent_WithValidData_IsAddedToDb()
        {
            const int expectedSearchRequestId = 2;
            var expectedSearchRequestEntity = SearchRequestEntityBuilder.NewRecord.Build();

            var searchRequestedEvent = new SearchRequestedEvent
            {
                SearchRequestId = expectedSearchRequestEntity.SearchRequestId,
                IsRepeatSearch = expectedSearchRequestEntity.IsRepeatSearch,
                OriginalSearchRequestId = expectedSearchRequestEntity.OriginalSearchRequestId,
                RepeatSearchCutOffDate = expectedSearchRequestEntity.RepeatSearchCutOffDate,
                RequestJson = expectedSearchRequestEntity.RequestJson,
                SearchCriteria = expectedSearchRequestEntity.SearchCriteria,
                DonorType = expectedSearchRequestEntity.DonorType,
                RequestTimeUtc = expectedSearchRequestEntity.RequestTimeUTC
            };

            await searchRequestRepository.TrackSearchRequestedEvent(searchRequestedEvent);

            var actualSearchRequestEntity = await searchRequestRepository.GetSearchRequestById(expectedSearchRequestId);

            expectedSearchRequestEntity.Should().BeEquivalentTo(actualSearchRequestEntity);
        }

        [Test]
        public async Task TrackMatchPredictionCompletedEvent_WhenCompleted_UpdatesSearchRequestInDb()
        {
            var expectedSearchRequestEntity = SearchRequestEntityBuilder.WithMatchingPredictionCompleted.Build();

            var matchPredictionCompletedEvent = new MatchPredictionCompletedEvent
            {
                SearchRequestId = expectedSearchRequestEntity.Id,
                CompletionDetails = new MatchPredictionCompletionDetails
                {
                    IsSuccessful = expectedSearchRequestEntity.MatchPrediction_IsSuccessful.Value,
                    FailureInfoJson = expectedSearchRequestEntity.MatchPrediction_FailureInfo_Json,
                    DonorsPerBatch = expectedSearchRequestEntity.MatchPrediction_DonorsPerBatch.Value,
                    TotalNumberOfBatches = expectedSearchRequestEntity.MatchPrediction_TotalNumberOfBatches.Value
                }
            };

            await searchRequestRepository.TrackMatchPredictionCompletedEvent(matchPredictionCompletedEvent);

            var actualSearchRequestEntity = await searchRequestRepository.GetSearchRequestById(expectedSearchRequestEntity.Id);

            expectedSearchRequestEntity.Should().BeEquivalentTo(actualSearchRequestEntity);
        }

        [Test]
        public async Task TrackMatchingAlgorithmCompletedEvent_WhenCompleted_UpdatesSearchRequestInDb()
        {
            var expectedSearchRequestEntity = SearchRequestEntityBuilder.WithMatchingAlgorithmCompleted.Build();

            var matchingAlgorithmCompletedEvent = new MatchingAlgorithmCompletedEvent
            {
                SearchRequestId = expectedSearchRequestEntity.Id,
                HlaNomenclatureVersion = expectedSearchRequestEntity.MatchingAlgorithm_HlaNomenclatureVersion,
                ResultsSent = expectedSearchRequestEntity.MatchingAlgorithm_ResultsSent.Value,
                ResultsSentTimeUtc = expectedSearchRequestEntity.MatchingAlgorithm_ResultsSentTimeUTC,
                CompletionDetails = new MatchingAlgorithmCompletionDetails
                {
                    IsSuccessful = expectedSearchRequestEntity.MatchingAlgorithm_IsSuccessful.Value,
                    FailureInfoJson = expectedSearchRequestEntity.MatchingAlgorithm_FailureInfo_Json,
                    TotalAttemptsNumber = expectedSearchRequestEntity.MatchingAlgorithm_TotalAttemptsNumber.Value,
                    NumberOfMatching = expectedSearchRequestEntity.MatchingAlgorithm_NumberOfMatching.Value,
                    NumberOfResults = expectedSearchRequestEntity.MatchingAlgorithm_NumberOfResults.Value,
                    RepeatSearchResultsDetails = new MatchingAlgorithmRepeatSearchResultsDetails
                    {
                        AddedResultCount = expectedSearchRequestEntity.MatchingAlgorithm_RepeatSearch_AddedResultCount.Value,
                        RemovedResultCount = expectedSearchRequestEntity.MatchingAlgorithm_RepeatSearch_RemovedResultCount.Value,
                        UpdatedResultCount = expectedSearchRequestEntity.MatchingAlgorithm_RepeatSearch_UpdatedResultCount.Value
                    }
                }
            };

            await searchRequestRepository.TrackMatchingAlgorithmCompletedEvent(matchingAlgorithmCompletedEvent);

            var actualSearchRequestEntity = await searchRequestRepository.GetSearchRequestById(expectedSearchRequestEntity.Id);

            expectedSearchRequestEntity.Should().BeEquivalentTo(actualSearchRequestEntity);
        }

        [Test]
        public async Task TrackSearchRequestCompletedEvent_WhenCompleted_UpdatesSearchRequestInDb()
        {
            var expectedSearchRequestEntity = SearchRequestEntityBuilder.WithSearchRequestCompleted.Build();

            var searchRequestCompletedEvent = new SearchRequestCompletedEvent
            {
                SearchRequestId = expectedSearchRequestEntity.Id,
                ResultsSent = expectedSearchRequestEntity.ResultsSent.Value,
                ResultsSentTimeUtc = expectedSearchRequestEntity.ResultsSentTimeUTC.Value
            };

            await searchRequestRepository.TrackSearchRequestCompletedEvent(searchRequestCompletedEvent);

            var actualSearchRequestEntity = await searchRequestRepository.GetSearchRequestById(expectedSearchRequestEntity.Id);

            expectedSearchRequestEntity.Should().BeEquivalentTo(actualSearchRequestEntity);
        }
    }
}
