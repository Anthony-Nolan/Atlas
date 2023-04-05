using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
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
                new Settings.Azure.AzureStorageSettings());

            batchedResultsSearchRunner = new SearchRunner(
                searchServiceBusClient,
                batchedResultsSearchService,
                resultsBlobStorageClient,
                logger,
                new MatchingAlgorithmSearchLoggingContext(),
                hlaNomenclatureVersionAccessor,
                new MessagingServiceBusSettings { SearchRequestsMaxDeliveryCount = MaxRetryCount },
                matchingFailureNotificationSender,
                new Settings.Azure.AzureStorageSettings { SearchResultsBatchSize = 1 });
        }

        [Test]
        public async Task RunSearch_RunsSearch()
        {
            var searchRequest = new SearchRequestBuilder().Build();

            await searchRunner.RunSearch(new IdentifiedSearchRequest { SearchRequest = searchRequest }, default);

            await searchService.Received().Search(searchRequest);
        }

        [Test]
        public async Task RunSearch_WhenResultsAreNotBatched_StoresResultsInBlobStorage()
        {
            const string id = "id";

            await searchRunner.RunSearch(new IdentifiedSearchRequest { Id = id, SearchRequest = DefaultMatchingRequest }, default);

            await resultsBlobStorageClient.Received().UploadResults(Arg.Is<ResultSet<MatchingAlgorithmResult>>(r => !r.BatchedResult), Arg.Any<int>(), id);
        }

        [Test]
        public async Task RunSearch_WhenResultsAreBatched_StoresResultsInBlobStorage()
        {
            const string id = "id";

            await batchedResultsSearchRunner.RunSearch(new IdentifiedSearchRequest { Id = id, SearchRequest = DefaultMatchingRequest }, default);

            await resultsBlobStorageClient.Received().UploadResults(Arg.Is<ResultSet<MatchingAlgorithmResult>>(r => r.BatchedResult), Arg.Any<int>(), id);
        }

        [Test]
        public async Task RunSearch_WhenResultsAreNotBatched_PublishesSuccessNotification()
        {
            const string id = "id";

            await searchRunner.RunSearch(new IdentifiedSearchRequest { Id = id, SearchRequest = DefaultMatchingRequest }, default);

            await searchServiceBusClient.Received().PublishToResultsNotificationTopic(Arg.Is<MatchingResultsNotification>(r =>
                r.WasSuccessful && r.SearchRequestId == id && !r.ResultsBatched && string.IsNullOrEmpty(r.BatchFolderName)
            ));
        }

        [Test]
        public async Task RunSearch_PublishesWmdaVersionInNotification()
        {
            const string hlaNomenclatureVersion = "hla-nomenclature-version";
            hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion().Returns(hlaNomenclatureVersion);

            await searchRunner.RunSearch(new IdentifiedSearchRequest { Id = "id", SearchRequest = DefaultMatchingRequest }, default);

            await searchServiceBusClient.Received().PublishToResultsNotificationTopic(Arg.Is<MatchingResultsNotification>(r =>
                r.MatchingAlgorithmHlaNomenclatureVersion == hlaNomenclatureVersion
            ));
        }

        [Test]
        public async Task RunSearch_WhenResultsAreBatched_PublishesSuccessNotificationWithBatchInfo()
        {
            var identifiedSearchRequest = new IdentifiedSearchRequest
            {
                Id = "id",
                SearchRequest = DefaultMatchingRequest
            };

            await batchedResultsSearchRunner.RunSearch(identifiedSearchRequest, default);

            await searchServiceBusClient.Received().PublishToResultsNotificationTopic(Arg.Is<MatchingResultsNotification>(r =>
                r.ResultsBatched && r.BatchFolderName.Equals(identifiedSearchRequest.Id)
            ));
        }

        [Test]
        public async Task RunSearch_WhenSearchFailsDueToInvalidHla_PublishesFailureNotificationWithCorrectFailureInfo()
        {
            const string id = "id";
            const int attemptNumber = 1;
            const string validationError = "bad hla";

            searchService.Search(default).ThrowsForAnyArgs(new HlaMetadataDictionaryException(Locus.A, "hla-name", validationError));

            try
            {
                await searchRunner.RunSearch(new IdentifiedSearchRequest { Id = id, SearchRequest = DefaultMatchingRequest }, attemptNumber);
            }
            catch (HlaMetadataDictionaryException)
            {
                // ignored
            }
            finally
            {
                await matchingFailureNotificationSender.Received().SendFailureNotification(id, attemptNumber, 0, validationError);
            }
        }

        [Test]
        public async Task RunSearch_WhenSearchFailsDueToInvalidHla_DoesNotRethrowException()
        {
            searchService.Search(default).ThrowsForAnyArgs(new AtlasHttpException(HttpStatusCode.InternalServerError, "dummy error message"));

            await searchRunner.Invoking(r => r.RunSearch(new IdentifiedSearchRequest { Id = "id", SearchRequest = DefaultMatchingRequest }, default))
                .Should().NotThrowAsync<HlaMetadataDictionaryException>();
        }

        [Test]
        public async Task RunSearch_WhenSearchFailsForOtherException_PublishesFailureNotificationWithCorrectFailureInfo()
        {
            const string id = "id";
            const int attemptNumber = 3;

            searchService.Search(default).ThrowsForAnyArgs(new Exception());

            try
            {
                await searchRunner.RunSearch(new IdentifiedSearchRequest { Id = id, SearchRequest = DefaultMatchingRequest }, attemptNumber);
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                await matchingFailureNotificationSender.Received().SendFailureNotification(id, attemptNumber, MaxRetryCount - attemptNumber);
            }
        }

        [Test]
        public async Task RunSearch_WhenSearchFailsForOtherException_RethrowsException()
        {
            searchService.Search(default).ThrowsForAnyArgs(new Exception());

            await searchRunner.Invoking(r => r.RunSearch(new IdentifiedSearchRequest { Id = "id", SearchRequest = DefaultMatchingRequest }, default))
                .Should().ThrowAsync<Exception>();
        }
    }
}