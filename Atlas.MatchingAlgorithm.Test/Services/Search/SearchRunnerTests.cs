using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.Client.Models.Search.Results.LogFile;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Utils.Http;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Clients.ServiceBus;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.MatchingAlgorithm.Settings.ServiceBus;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchRequests;
using Atlas.SearchTracking.Common.Models;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.Search
{
    [TestFixture]
    public class SearchRunnerTests
    {
        private const int MaxRetryCount = 10;
        private static SearchRequest DefaultMatchingRequest => new SearchRequestBuilder().Build();

        private ISearchServiceBusClient searchServiceBusClient;
        private ISearchService searchService;
        private ISearchService batchedResultsSearchService;
        private ISearchResultsBlobStorageClient resultsBlobStorageClient;
        private IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor;
        private IMatchingFailureNotificationSender matchingFailureNotificationSender;
        private IMatchingAlgorithmSearchTrackingContextManager matchingAlgorithmSearchTrackingContextManager;
        private IMatchingAlgorithmSearchTrackingDispatcher matchingAlgorithmSearchTrackingDispatcher;

        private ISearchRunner searchRunner;
        private ISearchRunner batchedResultsSearchRunner;

        [SetUp]
        public void SetUp()
        {
            searchServiceBusClient = Substitute.For<ISearchServiceBusClient>();
            searchService = Substitute.For<ISearchService>();
            resultsBlobStorageClient = Substitute.For<ISearchResultsBlobStorageClient>();
            hlaNomenclatureVersionAccessor = Substitute.For<IActiveHlaNomenclatureVersionAccessor>();
            var logger = Substitute.For<IMatchingAlgorithmSearchLogger>();
            matchingFailureNotificationSender = Substitute.For<IMatchingFailureNotificationSender>();
            matchingAlgorithmSearchTrackingContextManager = Substitute.For<IMatchingAlgorithmSearchTrackingContextManager>();
            matchingAlgorithmSearchTrackingDispatcher = Substitute.For<IMatchingAlgorithmSearchTrackingDispatcher>();

            batchedResultsSearchService = Substitute.For<ISearchService>();
            batchedResultsSearchService.Search(Arg.Any<SearchRequest>(), Arg.Any<DateTimeOffset?>())
                .Returns(new List<MatchingAlgorithmResult> { new MatchingAlgorithmResult() });

            searchRunner = new SearchRunner(
                searchServiceBusClient,
                searchService,
                resultsBlobStorageClient,
                logger,
                new MatchingAlgorithmSearchLoggingContext(),
                hlaNomenclatureVersionAccessor,
                new MessagingServiceBusSettings { SearchRequestsMaxDeliveryCount = MaxRetryCount },
                matchingFailureNotificationSender,
                new Settings.Azure.AzureStorageSettings(),
                matchingAlgorithmSearchTrackingContextManager,
                matchingAlgorithmSearchTrackingDispatcher);

            batchedResultsSearchRunner = new SearchRunner(
                searchServiceBusClient,
                batchedResultsSearchService,
                resultsBlobStorageClient,
                logger,
                new MatchingAlgorithmSearchLoggingContext(),
                hlaNomenclatureVersionAccessor,
                new MessagingServiceBusSettings { SearchRequestsMaxDeliveryCount = MaxRetryCount },
                matchingFailureNotificationSender,
                new Settings.Azure.AzureStorageSettings { SearchResultsBatchSize = 1 },
                matchingAlgorithmSearchTrackingContextManager,
                matchingAlgorithmSearchTrackingDispatcher);
        }

        [Test]
        public async Task RunSearch_RunsSearch()
        {
            const string id = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
            var searchRequest = new SearchRequestBuilder().Build();

            await searchRunner.RunSearch(new IdentifiedSearchRequest {Id = id, SearchRequest = searchRequest}, default, default);

            await searchService.Received().Search(searchRequest);
        }

        [Test]
        public async Task RunSearch_WhenResultsAreNotBatched_StoresResultsInBlobStorage()
        {
            const string id = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";

            await searchRunner.RunSearch(new IdentifiedSearchRequest { Id = id, SearchRequest = DefaultMatchingRequest }, default, default);

            await resultsBlobStorageClient.Received().UploadResults(Arg.Is<ResultSet<MatchingAlgorithmResult>>(r => !r.BatchedResult), Arg.Any<int>(), id);
        }

        [Test]
        public async Task RunSearch_WhenResultsAreBatched_StoresResultsInBlobStorage()
        {
            const string id = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";

            await batchedResultsSearchRunner.RunSearch(new IdentifiedSearchRequest { Id = id, SearchRequest = DefaultMatchingRequest }, default, default);

            await resultsBlobStorageClient.Received().UploadResults(Arg.Is<ResultSet<MatchingAlgorithmResult>>(r => r.BatchedResult), Arg.Any<int>(), id);
        }

        [Test]
        public async Task RunSearch_WhenResultsAreNotBatched_PublishesSuccessNotification()
        {
            const string id = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";

            await searchRunner.RunSearch(new IdentifiedSearchRequest { Id = id, SearchRequest = DefaultMatchingRequest }, default, default);

            await searchServiceBusClient.Received().PublishToResultsNotificationTopic(Arg.Is<MatchingResultsNotification>(r =>
                r.WasSuccessful && r.SearchRequestId == id && !r.ResultsBatched && string.IsNullOrEmpty(r.BatchFolderName)
            ));
        }

        [Test]
        public async Task RunSearch_PublishesWmdaVersionInNotification()
        {
            const string id = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
            const string hlaNomenclatureVersion = "hla-nomenclature-version";
            hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion().Returns(hlaNomenclatureVersion);

            await searchRunner.RunSearch(new IdentifiedSearchRequest { Id = id, SearchRequest = DefaultMatchingRequest }, default, default);

            await searchServiceBusClient.Received().PublishToResultsNotificationTopic(Arg.Is<MatchingResultsNotification>(r =>
                r.MatchingAlgorithmHlaNomenclatureVersion == hlaNomenclatureVersion
            ));
        }

        [Test]
        public async Task RunSearch_WhenResultsAreBatched_PublishesSuccessNotificationWithBatchInfo()
        {
            const string id = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
            var identifiedSearchRequest = new IdentifiedSearchRequest
            {
                Id = id,
                SearchRequest = DefaultMatchingRequest
            };

            await batchedResultsSearchRunner.RunSearch(identifiedSearchRequest, default, default);

            await searchServiceBusClient.Received().PublishToResultsNotificationTopic(Arg.Is<MatchingResultsNotification>(r =>
                r.ResultsBatched && r.BatchFolderName.Equals(identifiedSearchRequest.Id)
            ));
        }

        [Test]
        public async Task RunSearch_WhenSearchFailsDueToInvalidHla_PublishesFailureNotificationWithCorrectFailureInfo()
        {
            const string id = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
            const int attemptNumber = 1;
            const string validationError = "bad hla";

            searchService.Search(default).ThrowsForAnyArgs(new HlaMetadataDictionaryException(Locus.A, "hla-name", validationError));

            var request = new IdentifiedSearchRequest { Id = id, SearchRequest = DefaultMatchingRequest };

            try
            {
                await searchRunner.RunSearch(request, attemptNumber, default);
            }
            catch (HlaMetadataDictionaryException)
            {
                // ignored
            }
            finally
            {
                await matchingFailureNotificationSender.Received().SendFailureNotification(
                    Arg.Is<IdentifiedSearchRequest>(x => x.Id == id),
                    attemptNumber,
                    0,
                    validationError);
            }
        }

        [Test]
        public async Task RunSearch_WhenSearchFailsDueToValidationError_DoesNotRethrowException()
        {
            const string id = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
            searchService.Search(default).ThrowsForAnyArgs(new AtlasHttpException(HttpStatusCode.InternalServerError, "dummy error message"));

            await searchRunner.Invoking(r => r.RunSearch(new IdentifiedSearchRequest { Id = id, SearchRequest = DefaultMatchingRequest }, default, default))
                .Should().NotThrowAsync<FluentValidation.ValidationException>();
        }

        [Test]
        public async Task RunSearch_WhenSearchFailsDueToInvalidHla_DoesNotRethrowException()
        {
            const string id = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
            searchService.Search(default).ThrowsForAnyArgs(new AtlasHttpException(HttpStatusCode.InternalServerError, "dummy error message"));

            await searchRunner.Invoking(r => r.RunSearch(new IdentifiedSearchRequest { Id = id, SearchRequest = DefaultMatchingRequest }, default, default))
                .Should().NotThrowAsync<HlaMetadataDictionaryException>();
        }

        [Test]
        public async Task RunSearch_WhenSearchFailsForOtherException_PublishesFailureNotificationWithCorrectFailureInfo()
        {
            const string id = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
            const int attemptNumber = 3;

            searchService.Search(default).ThrowsForAnyArgs(new Exception());

            var request = new IdentifiedSearchRequest { Id = id, SearchRequest = DefaultMatchingRequest };

            try
            {
                await searchRunner.RunSearch(request, attemptNumber, default);
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                await matchingFailureNotificationSender.Received().SendFailureNotification(
                    Arg.Is<IdentifiedSearchRequest>(x => x.Id == id),
                    attemptNumber,
                    MaxRetryCount - attemptNumber);
            }
        }

        [Test]
        public async Task RunSearch_WhenSearchFailsForOtherException_RethrowsException()
        {
            const string id = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
            searchService.Search(default).ThrowsForAnyArgs(new Exception());

            await searchRunner.Invoking(r => r.RunSearch(new IdentifiedSearchRequest { Id = id, SearchRequest = DefaultMatchingRequest }, default, default))
                .Should().ThrowAsync<Exception>();
        }

        [Test]
        public async Task RunSearch_StorePerformanceLogsInBlobStorage()
        {
            const string searchRequestId = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
            await searchRunner.RunSearch(new IdentifiedSearchRequest { SearchRequest = DefaultMatchingRequest, Id = searchRequestId }, default, default);

            await resultsBlobStorageClient.Received().UploadResults(
                Arg.Is<SearchLog>(x => x.SearchRequestId == searchRequestId && x.WasSuccessful),
                Arg.Any<string>(),
                $"{searchRequestId}-log.json");
        }

        [Test]
        public async Task RunSearch_WhenSearchFailsForException_StorePerformanceLogsInBlobStorage()
        {
            const string searchRequestId = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
            searchService.Search(default).ThrowsForAnyArgs(new Exception());

            try
            {
                await searchRunner.RunSearch(new IdentifiedSearchRequest { SearchRequest = DefaultMatchingRequest, Id = searchRequestId }, default,
                    default);
            }
            catch (Exception ex)
            {
                // ignored
            }
            finally
            {
                await resultsBlobStorageClient.Received().UploadResults(
                    Arg.Is<SearchLog>(x => x.SearchRequestId == searchRequestId && !x.WasSuccessful),
                    Arg.Any<string>(),
                    $"{searchRequestId}-log.json");
            }
        }


        [Test]
        public async Task RunSearch_WhenLogsUploadFails_DoesNotThrowException()
        {
            const string id = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
            resultsBlobStorageClient.UploadResults(Arg.Any<SearchLog>(), default, default).ThrowsForAnyArgs(new Exception());

            await searchRunner.Invoking(r => r.RunSearch(new IdentifiedSearchRequest { Id = id, SearchRequest = DefaultMatchingRequest }, default, default))
                .Should().NotThrowAsync<Exception>();
        }

        [Test]
        public async Task RunSearch_WhenSearchFailsDueToInvalidHla_DispatchesSearchTrackingEvent()
        {
            const string id = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
            const int attemptNumber = 1;
            const string validationError = "bad hla";

            searchService.Search(default).ThrowsForAnyArgs(new HlaMetadataDictionaryException(Locus.A, "hla-name", validationError));

            var request = new IdentifiedSearchRequest { Id = id, SearchRequest = DefaultMatchingRequest };

            try
            {
                await searchRunner.RunSearch(request, attemptNumber, default);
            }
            catch (HlaMetadataDictionaryException)
            {
                // ignored
            }
            finally
            {
                await matchingAlgorithmSearchTrackingDispatcher.Received().ProcessCompleted(
                    Arg.Any<MatchingAlgorithmCompletedEvent>());
            }
        }

        [Test]
        public async Task RunSearch_WhenSearchFailsForOtherException_DispatchesSearchTrackingEvent()
        {
            const string id = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
            const int attemptNumber = 3;

            searchService.Search(default).ThrowsForAnyArgs(new Exception());

            var request = new IdentifiedSearchRequest { Id = id, SearchRequest = DefaultMatchingRequest };

            try
            {
                await searchRunner.RunSearch(request, attemptNumber, default);
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                await matchingAlgorithmSearchTrackingDispatcher.Received().ProcessCompleted(
                    Arg.Any<MatchingAlgorithmCompletedEvent>());
            }
        }
    }
}