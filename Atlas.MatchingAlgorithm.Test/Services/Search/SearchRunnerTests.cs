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
        private IActiveHlaVersionAccessor hlaVersionAccessor;

        private ISearchRunner searchRunner;

        [SetUp]
        public void SetUp()
        {
            searchServiceBusClient = Substitute.For<ISearchServiceBusClient>();
            searchService = Substitute.For<ISearchService>();
            resultsBlobStorageClient = Substitute.For<IResultsBlobStorageClient>();
            hlaVersionAccessor = Substitute.For<IActiveHlaVersionAccessor>();
            var logger = Substitute.For<ILogger>();
            var context = Substitute.For<ISearchRequestContext>();

            searchRunner = new SearchRunner(
                searchServiceBusClient,
                searchService,
                resultsBlobStorageClient,
                logger,
                context,
                hlaVersionAccessor);
        }

        [Test]
        public async Task RunSearch_RunsSearch()
        {
            var searchRequest = new SearchRequest();

            await searchRunner.RunSearch(new IdentifiedSearchRequest { SearchRequest = searchRequest });

            await searchService.Received().Search(searchRequest);
        }

        [Test]
        public async Task RunSearch_StoresResultsInBlobStorage()
        {
            const string id = "id";

            await searchRunner.RunSearch(new IdentifiedSearchRequest { Id = id });

            await resultsBlobStorageClient.Received().UploadResults(id, Arg.Any<SearchResultSet>());
        }

        [Test]
        public async Task RunSearch_PublishesSuccessNotification()
        {
            const string id = "id";

            await searchRunner.RunSearch(new IdentifiedSearchRequest { Id = id });

            await searchServiceBusClient.PublishToResultsNotificationTopic(Arg.Is<SearchResultsNotification>(r =>
                r.WasSuccessful && r.SearchRequestId == id
            ));
        }

        [Test]
        public async Task RunSearch_PublishesWmdaVersionInNotification()
        {
            const string hlaNomenclatureVersion = "hla-nomenclature-version";
            hlaVersionAccessor.GetActiveHlaNomenclatureVersion().Returns(hlaNomenclatureVersion);

            await searchRunner.RunSearch(new IdentifiedSearchRequest { Id = "id" });

            await searchServiceBusClient.PublishToResultsNotificationTopic(Arg.Is<SearchResultsNotification>(r =>
                r.HlaNomenclatureVersion == hlaNomenclatureVersion
            ));
        }

        [Test]
        public async Task RunSearch_WhenSearchFails_PublishesFailureNotification()
        {
            const string id = "id";
            searchService.Search(Arg.Any<SearchRequest>()).Throws(new AtlasHttpException(HttpStatusCode.InternalServerError, ""));

            await searchRunner.RunSearch(new IdentifiedSearchRequest { Id = id });

            await searchServiceBusClient.PublishToResultsNotificationTopic(Arg.Is<SearchResultsNotification>(r =>
                !r.WasSuccessful && r.SearchRequestId == id
            ));
        }
    }
}