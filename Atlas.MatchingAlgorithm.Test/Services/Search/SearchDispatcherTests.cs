using FluentValidation;
using Atlas.MatchingAlgorithm.ApplicationInsights.SearchRequests;
using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;
using Atlas.MatchingAlgorithm.Client.Models.SearchResults;
using Atlas.MatchingAlgorithm.Clients.AzureStorage;
using Atlas.MatchingAlgorithm.Clients.ServiceBus;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.MatchingAlgorithm.Test.Builders;
using Nova.Utils.ApplicationInsights;
using Atlas.Utils.Core.Http.Exceptions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using System.Net;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Test.Services.Search
{
    [TestFixture]
    public class SearchDispatcherTests
    {
        private ISearchServiceBusClient searchServiceBusClient;
        private ISearchService searchService;
        private IResultsBlobStorageClient resultsBlobStorageClient;
        private IWmdaHlaVersionProvider wmdaHlaVersionProvider;

        private SearchDispatcher searchDispatcher;

        [SetUp]
        public void SetUp()
        {
            searchServiceBusClient = Substitute.For<ISearchServiceBusClient>();
            searchService = Substitute.For<ISearchService>();
            resultsBlobStorageClient = Substitute.For<IResultsBlobStorageClient>();
            wmdaHlaVersionProvider = Substitute.For<IWmdaHlaVersionProvider>();
            var logger = Substitute.For<ILogger>();
            var context = Substitute.For<ISearchRequestContext>();

            searchDispatcher = new SearchDispatcher(
                searchServiceBusClient,
                searchService,
                resultsBlobStorageClient,
                logger,
                context,
                wmdaHlaVersionProvider);
        }

        [Test]
        public async Task DispatchSearch_DispatchesSearchWithId()
        {
            await searchDispatcher.DispatchSearch(
                new SearchRequestBuilder()
                    .WithSearchHla(new PhenotypeInfo<string>("hla-type"))
                    .WithTotalMismatchCount(0)
                    .Build());

            await searchServiceBusClient.Received().PublishToSearchQueue(Arg.Is<IdentifiedSearchRequest>(r => r.Id != null));
        }

        [Test]
        public void DispatchSearch_ValidatesSearchRequest()
        {
            var invalidSearchRequest = new SearchRequest();

            Assert.ThrowsAsync<ValidationException>(() => searchDispatcher.DispatchSearch(invalidSearchRequest));
        }

        [Test]
        public async Task RunSearch_RunsSearch()
        {
            var searchRequest = new SearchRequest();

            await searchDispatcher.RunSearch(new IdentifiedSearchRequest { SearchRequest = searchRequest });

            await searchService.Received().Search(searchRequest);
        }

        [Test]
        public async Task RunSearch_StoresResultsInBlobStorage()
        {
            const string id = "id";

            await searchDispatcher.RunSearch(new IdentifiedSearchRequest { Id = id });

            await resultsBlobStorageClient.Received().UploadResults(id, Arg.Any<SearchResultSet>());
        }

        [Test]
        public async Task RunSearch_PublishesSuccessNotification()
        {
            const string id = "id";

            await searchDispatcher.RunSearch(new IdentifiedSearchRequest { Id = id });

            await searchServiceBusClient.PublishToResultsNotificationTopic(Arg.Is<SearchResultsNotification>(r =>
                r.WasSuccessful && r.SearchRequestId == id
            ));
        }

        [Test]
        public async Task RunSearch_PublishesWmdaVersionInNotification()
        {
            const string wmdaVersion = "wmda-version";
            wmdaHlaVersionProvider.GetActiveHlaDatabaseVersion().Returns(wmdaVersion);

            await searchDispatcher.RunSearch(new IdentifiedSearchRequest { Id = "id" });

            await searchServiceBusClient.PublishToResultsNotificationTopic(Arg.Is<SearchResultsNotification>(r =>
                r.WmdaHlaDatabaseVersion == wmdaVersion
            ));
        }

        [Test]
        public async Task RunSearch_WhenSearchFails_PublishesFailureNotification()
        {
            const string id = "id";
            searchService.Search(Arg.Any<SearchRequest>()).Throws(new AtlasHttpException(HttpStatusCode.InternalServerError, ""));

            await searchDispatcher.RunSearch(new IdentifiedSearchRequest { Id = id });

            await searchServiceBusClient.PublishToResultsNotificationTopic(Arg.Is<SearchResultsNotification>(r =>
                !r.WasSuccessful && r.SearchRequestId == id
            ));
        }
    }
}