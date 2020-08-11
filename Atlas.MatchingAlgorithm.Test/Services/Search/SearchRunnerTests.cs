using System.Net;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Utils.Http;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;
using Atlas.MatchingAlgorithm.Client.Models.SearchResults;
using Atlas.MatchingAlgorithm.Clients.AzureStorage;
using Atlas.MatchingAlgorithm.Clients.ServiceBus;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.Search
{
    [TestFixture]
    public class SearchRunnerTests
    {
        private MatchingRequest DefaultMatchingRequest => new MatchingRequestBuilder().Build();

        private ISearchServiceBusClient searchServiceBusClient;
        private ISearchService searchService;
        private IResultsBlobStorageClient resultsBlobStorageClient;
        private IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor;

        private ISearchRunner searchRunner;

        [SetUp]
        public void SetUp()
        {
            searchServiceBusClient = Substitute.For<ISearchServiceBusClient>();
            searchService = Substitute.For<ISearchService>();
            resultsBlobStorageClient = Substitute.For<IResultsBlobStorageClient>();
            hlaNomenclatureVersionAccessor = Substitute.For<IActiveHlaNomenclatureVersionAccessor>();
            var logger = Substitute.For<IMatchingAlgorithmSearchLogger>();

            searchRunner = new SearchRunner(
                searchServiceBusClient,
                searchService,
                resultsBlobStorageClient,
                logger,
                new MatchingAlgorithmSearchLoggingContext(),
                hlaNomenclatureVersionAccessor);
        }

        [Test]
        public async Task RunSearch_RunsSearch()
        {
            var searchRequest = new MatchingRequestBuilder().Build();

            await searchRunner.RunSearch(new IdentifiedSearchRequest {MatchingRequest = searchRequest});

            await searchService.Received().Search(searchRequest);
        }

        [Test]
        public async Task RunSearch_StoresResultsInBlobStorage()
        {
            const string id = "id";

            await searchRunner.RunSearch(new IdentifiedSearchRequest {Id = id, MatchingRequest = DefaultMatchingRequest});

            await resultsBlobStorageClient.Received().UploadResults(Arg.Any<MatchingAlgorithmResultSet>());
        }

        [Test]
        public async Task RunSearch_PublishesSuccessNotification()
        {
            const string id = "id";

            await searchRunner.RunSearch(new IdentifiedSearchRequest {Id = id, MatchingRequest = DefaultMatchingRequest});

            await searchServiceBusClient.PublishToResultsNotificationTopic(Arg.Is<MatchingResultsNotification>(r =>
                r.WasSuccessful && r.SearchRequestId == id
            ));
        }

        [Test]
        public async Task RunSearch_PublishesWmdaVersionInNotification()
        {
            const string hlaNomenclatureVersion = "hla-nomenclature-version";
            hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion().Returns(hlaNomenclatureVersion);

            await searchRunner.RunSearch(new IdentifiedSearchRequest {Id = "id", MatchingRequest = DefaultMatchingRequest});

            await searchServiceBusClient.PublishToResultsNotificationTopic(Arg.Is<MatchingResultsNotification>(r =>
                r.HlaNomenclatureVersion == hlaNomenclatureVersion
            ));
        }

        [Test]
        public async Task RunSearch_WhenSearchFails_PublishesFailureNotification()
        {
            const string id = "id";
            searchService.Search(default).ThrowsForAnyArgs(new AtlasHttpException(HttpStatusCode.InternalServerError, "dummy error message"));

            try
            {
                await searchRunner.RunSearch(new IdentifiedSearchRequest {Id = id, MatchingRequest = DefaultMatchingRequest});
            }
            catch (AtlasHttpException)
            {
            }
            finally
            {
                await searchServiceBusClient.PublishToResultsNotificationTopic(Arg.Is<MatchingResultsNotification>(r =>
                    !r.WasSuccessful && r.SearchRequestId == id
                ));
            }
        }

        [Test]
        public async Task RunSearch_WhenSearchFails_ReThrowsException()
        {
            searchService.Search(default).ThrowsForAnyArgs(new AtlasHttpException(HttpStatusCode.InternalServerError, "dummy error message"));

            await searchRunner.Invoking(r => r.RunSearch(new IdentifiedSearchRequest {Id = "id", MatchingRequest = DefaultMatchingRequest}))
                .Should().ThrowAsync<AtlasHttpException>();
        }
    }
}