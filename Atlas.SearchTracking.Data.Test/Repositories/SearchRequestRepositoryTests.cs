﻿using Atlas.SearchTracking.Common.Models;
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
            var expectedSearchRequestId = new Guid("aaaaaaaa-7777-8888-9999-000000000000");

            var act = async () => await searchRequestRepository.GetSearchRequestByGuid(expectedSearchRequestId);

            act.Should().Throw<Exception>().WithMessage($"Search request with Guid {expectedSearchRequestId} not found");
        }

        [Test]
        public async Task TrackSearchRequestedEvent_WithValidData_IsAddedToDb()
        {
            var expectedSearchRequestId = new Guid("eeeeeeee-bbbb-cccc-dddd-000000000000");
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
                RequestTimeUtc = expectedSearchRequestEntity.RequestTimeUtc,
                AreBetterMatchesIncluded = expectedSearchRequestEntity.AreBetterMatchesIncluded,
                IsMatchPredictionRun = expectedSearchRequestEntity.IsMatchPredictionRun,
                DonorRegistryCodes = expectedSearchRequestEntity.DonorRegistryCodes
            };

            await searchRequestRepository.TrackSearchRequestedEvent(searchRequestedEvent);

            var actualSearchRequestEntity = await searchRequestRepository.GetSearchRequestByGuid(expectedSearchRequestId);

            expectedSearchRequestEntity.Should().BeEquivalentTo(actualSearchRequestEntity);
        }

        [Test]
        public async Task TrackMatchPredictionCompletedEvent_WhenCompleted_UpdatesSearchRequestInDb()
        {
            var expectedSearchRequestEntity = SearchRequestEntityBuilder.WithMatchingPredictionCompleted.Build();

            var matchPredictionCompletedEvent = new MatchPredictionCompletedEvent
            {
                SearchRequestId = expectedSearchRequestEntity.SearchRequestId,
                CompletionDetails = new MatchPredictionCompletionDetails
                {
                    IsSuccessful = expectedSearchRequestEntity.MatchPrediction_IsSuccessful.Value,
                    FailureInfo = new MatchPredictionFailureInfo
                    {
                        Message = expectedSearchRequestEntity.MatchPrediction_FailureInfo_Message,
                        ExceptionStacktrace = expectedSearchRequestEntity.MatchPrediction_FailureInfo_ExceptionStacktrace,
                        Type = expectedSearchRequestEntity.MatchPrediction_FailureInfo_Type.Value
                    },
                    DonorsPerBatch = expectedSearchRequestEntity.MatchPrediction_DonorsPerBatch.Value,
                    TotalNumberOfBatches = expectedSearchRequestEntity.MatchPrediction_TotalNumberOfBatches.Value
                }
            };

            await searchRequestRepository.TrackMatchPredictionCompletedEvent(matchPredictionCompletedEvent);

            var actualSearchRequestEntity = await searchRequestRepository.GetSearchRequestByGuid(expectedSearchRequestEntity.SearchRequestId);

            expectedSearchRequestEntity.Should().BeEquivalentTo(actualSearchRequestEntity);
        }

        [Test]
        public async Task TrackMatchingAlgorithmCompletedEvent_WhenCompleted_UpdatesSearchRequestInDb()
        {
            var expectedSearchRequestEntity = SearchRequestEntityBuilder.WithMatchingAlgorithmCompleted.Build();

            var matchingAlgorithmCompletedEvent = new MatchingAlgorithmCompletedEvent
            {
                SearchRequestId = expectedSearchRequestEntity.SearchRequestId,
                HlaNomenclatureVersion = expectedSearchRequestEntity.MatchingAlgorithm_HlaNomenclatureVersion,
                ResultsSent = expectedSearchRequestEntity.MatchingAlgorithm_ResultsSent.Value,
                ResultsSentTimeUtc = expectedSearchRequestEntity.MatchingAlgorithm_ResultsSentTimeUtc,
                CompletionDetails = new MatchingAlgorithmCompletionDetails
                {
                    IsSuccessful = expectedSearchRequestEntity.MatchingAlgorithm_IsSuccessful.Value,
                    FailureInfo = new MatchingAlgorithmFailureInfo
                    {
                        Message = expectedSearchRequestEntity.MatchingAlgorithm_FailureInfo_Message,
                        ExceptionStacktrace = expectedSearchRequestEntity.MatchingAlgorithm_FailureInfo_ExceptionStacktrace,
                        Type = expectedSearchRequestEntity.MatchingAlgorithm_FailureInfo_Type
                    },
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

            var actualSearchRequestEntity = await searchRequestRepository.GetSearchRequestByGuid(expectedSearchRequestEntity.SearchRequestId);

            expectedSearchRequestEntity.Should().BeEquivalentTo(actualSearchRequestEntity);
        }

        [Test]
        public async Task TrackSearchRequestCompletedEvent_WhenCompleted_UpdatesSearchRequestInDb()
        {
            var expectedSearchRequestEntity = SearchRequestEntityBuilder.WithSearchRequestCompleted.Build();

            var searchRequestCompletedEvent = new SearchRequestCompletedEvent
            {
                SearchRequestId = expectedSearchRequestEntity.SearchRequestId,
                ResultsSent = expectedSearchRequestEntity.ResultsSent.Value,
                ResultsSentTimeUtc = expectedSearchRequestEntity.ResultsSentTimeUtc.Value
            };

            await searchRequestRepository.TrackSearchRequestCompletedEvent(searchRequestCompletedEvent);

            var actualSearchRequestEntity = await searchRequestRepository.GetSearchRequestByGuid(expectedSearchRequestEntity.SearchRequestId);

            expectedSearchRequestEntity.Should().BeEquivalentTo(actualSearchRequestEntity);
        }
    }
}
