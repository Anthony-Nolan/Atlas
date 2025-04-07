using Atlas.SearchTracking.Common.Enums;
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
        private readonly Guid searchRequestId = new Guid("aaaaaaaa-bbbb-cccc-dddd-000000000000");

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

            var act = async () => await searchRequestRepository.GetSearchRequestByIdentifier(expectedSearchRequestId);

            act.Should().Throw<Exception>().WithMessage($"Search request with identifier {expectedSearchRequestId} not found");
        }

        [Test]
        public async Task TrackSearchRequestedEvent_WithValidData_IsAddedToDb()
        {
            var newSearchRequestId = new Guid("eeeeeeee-bbbb-cccc-dddd-000000000000");
            var expectedSearchRequestEntity = SearchRequestEntityBuilder.NewRecord.Build();

            var searchRequestedEvent = new SearchRequestedEvent
            {
                SearchIdentifier = expectedSearchRequestEntity.SearchIdentifier,
                IsRepeatSearch = expectedSearchRequestEntity.IsRepeatSearch,
                OriginalSearchIdentifier = expectedSearchRequestEntity.OriginalSearchIdentifier,
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

            var actualSearchRequestEntity = await searchRequestRepository.GetSearchRequestByIdentifier(newSearchRequestId);

            actualSearchRequestEntity.Should().BeEquivalentTo(expectedSearchRequestEntity);
        }

        [Test]
        public async Task TrackMatchPredictionCompletedEvent_WhenCompleted_UpdatesSearchRequestInDb()
        {
            var expectedSearchRequestEntity = SearchRequestEntityBuilder.WithMatchingPredictionCompleted.Build();

            var matchPredictionCompletedEvent = new MatchPredictionCompletedEvent
            {
                SearchIdentifier = searchRequestId,
                CompletionDetails = new MatchPredictionCompletionDetails
                {
                    IsSuccessful = true,
                    DonorsPerBatch = 10,
                    TotalNumberOfBatches = 1
                }
            };

            await searchRequestRepository.TrackMatchPredictionCompletedEvent(matchPredictionCompletedEvent);

            var actualSearchRequestEntity = await searchRequestRepository.GetSearchRequestByIdentifier(searchRequestId);

            actualSearchRequestEntity.Should().BeEquivalentTo(expectedSearchRequestEntity);
        }

        [Test]
        public async Task TrackMatchPredictionCompletedEvent_WhenNotCompleted_UpdatesSearchRequestInDb()
        {
            var expectedSearchRequestEntity = SearchRequestEntityBuilder.WithMatchingPredictionNotCompleted.Build();

            var matchPredictionCompletedEvent = new MatchPredictionCompletedEvent
            {
                SearchIdentifier = searchRequestId,
                CompletionDetails = new MatchPredictionCompletionDetails
                {
                    IsSuccessful = false,
                    FailureInfo = new MatchPredictionFailureInfo
                    {
                        Message = "FailureInfoMessage",
                        ExceptionStacktrace = "StackTrace",
                        Type = MatchPredictionFailureType.UnexpectedError
                    }
                }
            };

            await searchRequestRepository.TrackMatchPredictionCompletedEvent(matchPredictionCompletedEvent);

            var actualSearchRequestEntity = await searchRequestRepository.GetSearchRequestByIdentifier(expectedSearchRequestEntity.SearchIdentifier);

            actualSearchRequestEntity.Should().BeEquivalentTo(expectedSearchRequestEntity);
        }


        [Test]
        public async Task TrackMatchingAlgorithmCompletedEvent_WhenCompleted_UpdatesSearchRequestInDb()
        {
            var expectedSearchRequestEntity = SearchRequestEntityBuilder.WithMatchingAlgorithmCompleted.Build();

            var matchingAlgorithmCompletedEvent = new MatchingAlgorithmCompletedEvent
            {
                SearchIdentifier = searchRequestId,
                HlaNomenclatureVersion = "3.6.0",
                ResultsSent = true,
                ResultsSentTimeUtc = new DateTime(2023, 1, 1),
                CompletionDetails = new MatchingAlgorithmCompletionDetails
                {
                    IsSuccessful = true,
                    TotalAttemptsNumber = 3,
                    NumberOfResults = 2000,
                    RepeatSearchResultsDetails = new MatchingAlgorithmRepeatSearchResultsDetails
                    {
                        AddedResultCount = 50,
                        RemovedResultCount = 10,
                        UpdatedResultCount = 5
                    }
                }
            };

            await searchRequestRepository.TrackMatchingAlgorithmCompletedEvent(matchingAlgorithmCompletedEvent);

            var actualSearchRequestEntity = await searchRequestRepository.GetSearchRequestByIdentifier(searchRequestId);

            actualSearchRequestEntity.Should().BeEquivalentTo(expectedSearchRequestEntity);
        }

        [Test]
        public async Task TrackMatchingAlgorithmCompletedEvent_WhenNotCompleted_UpdatesSearchRequestInDb()
        {
            var expectedSearchRequestEntity = SearchRequestEntityBuilder.WithMatchingAlgorithmNotCompleted.Build();

            var matchingAlgorithmCompletedEvent = new MatchingAlgorithmCompletedEvent
            {
                SearchIdentifier = searchRequestId,
                CompletionDetails = new MatchingAlgorithmCompletionDetails
                {
                    TotalAttemptsNumber = 0,
                    FailureInfo = new MatchingAlgorithmFailureInfo
                    {
                        Message = "FailureInfoMessage",
                        ExceptionStacktrace = "StackTrace",
                        Type = MatchingAlgorithmFailureType.ValidationError
                    }
                }
            };

            await searchRequestRepository.TrackMatchingAlgorithmCompletedEvent(matchingAlgorithmCompletedEvent);

            var actualSearchRequestEntity = await searchRequestRepository.GetSearchRequestByIdentifier(expectedSearchRequestEntity.SearchIdentifier);

            actualSearchRequestEntity.Should().BeEquivalentTo(expectedSearchRequestEntity);
        }

        [Test]
        public async Task TrackSearchRequestCompletedEvent_WhenCompleted_UpdatesSearchRequestInDb()
        {
            var expectedSearchRequestEntity = SearchRequestEntityBuilder.WithSearchRequestCompleted.Build();

            var searchRequestCompletedEvent = new SearchRequestCompletedEvent
            {
                SearchIdentifier = searchRequestId,
                ResultsSent = true,
                ResultsSentTimeUtc = new DateTime(2023, 1, 1)
            };

            await searchRequestRepository.TrackSearchRequestCompletedEvent(searchRequestCompletedEvent);

            var actualSearchRequestEntity = await searchRequestRepository.GetSearchRequestByIdentifier(expectedSearchRequestEntity.SearchIdentifier);

            actualSearchRequestEntity.Should().BeEquivalentTo(expectedSearchRequestEntity);
        }
    }
}
