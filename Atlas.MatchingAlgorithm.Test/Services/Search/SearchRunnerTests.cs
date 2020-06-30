using System.Net;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Utils.Http;
using Atlas.MatchingAlgorithm.ApplicationInsights.SearchRequests;
using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;
using Atlas.MatchingAlgorithm.Client.Models.SearchResults;
using Atlas.MatchingAlgorithm.Clients.AzureStorage;
using Atlas.MatchingAlgorithm.Clients.ServiceBus;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.Search;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.Search
{
    [TestFixture]
    public class SearchRunnerTests
    {
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
            var logger = Substitute.For<ILogger>();
            var context = Substitute.For<ISearchRequestContext>();

            searchRunner = new SearchRunner(
                searchServiceBusClient,
                searchService,
                resultsBlobStorageClient,
                logger,
                context,
                hlaNomenclatureVersionAccessor);
        }

        [Test]
        public async Task RunSearch_RunsSearch()
        {
            var searchRequest = new SearchRequest();

            await searchRunner.RunSearch(new IdentifiedSearchRequest {SearchRequest = searchRequest});

            await searchService.Received().Search(searchRequest);
        }

        [Test]
        public async Task RunSearch_StoresResultsInBlobStorage()
        {
            const string id = "id";

            await searchRunner.RunSearch(new IdentifiedSearchRequest {Id = id});

            await resultsBlobStorageClient.Received().UploadResults(id, Arg.Any<SearchResultSet>());
        }

        [Test]
        public async Task RunSearch_PublishesSuccessNotification()
        {
            const string id = "id";

            await searchRunner.RunSearch(new IdentifiedSearchRequest {Id = id});

            await searchServiceBusClient.PublishToResultsNotificationTopic(Arg.Is<SearchResultsNotification>(r =>
                r.WasSuccessful && r.SearchRequestId == id
            ));
        }

        [Test]
        public async Task RunSearch_PublishesWmdaVersionInNotification()
        {
            const string hlaNomenclatureVersion = "hla-nomenclature-version";
            hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion().Returns(hlaNomenclatureVersion);

            await searchRunner.RunSearch(new IdentifiedSearchRequest {Id = "id"});

            await searchServiceBusClient.PublishToResultsNotificationTopic(Arg.Is<SearchResultsNotification>(r =>
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
                await searchRunner.RunSearch(new IdentifiedSearchRequest {Id = id});
            }
            catch (AtlasHttpException)
            {
            }
            finally
            {
                await searchServiceBusClient.PublishToResultsNotificationTopic(Arg.Is<SearchResultsNotification>(r =>
                    !r.WasSuccessful && r.SearchRequestId == id
                ));
            }
        }

        [Test]
        public async Task RunSearch_WhenSearchFails_ReThrowsException()
        {
            searchService.Search(default).ThrowsForAnyArgs(new AtlasHttpException(HttpStatusCode.InternalServerError, "dummy error message"));

            await searchRunner.Invoking(r => r.RunSearch(new IdentifiedSearchRequest{ Id = "id"})).Should().ThrowAsync<AtlasHttpException>();
        }
    }
}