using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Client.Models.SearchRequests;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Clients;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Services.AzureStorage;
using Nova.SearchAlgorithm.Services.Search;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.Http.Exceptions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Services.Search
{
    [TestFixture]
    public class SearchDispatcherTests
    {
        private ISearchServiceBusClient searchServiceBusClient;
        private ISearchService searchService;
        private IBlobStorageClient blobStorageClient;

        private SearchDispatcher searchDispatcher;

        [SetUp]
        public void SetUp()
        {
            searchServiceBusClient = Substitute.For<ISearchServiceBusClient>();
            searchService = Substitute.For<ISearchService>();
            blobStorageClient = Substitute.For<IBlobStorageClient>();
            var logger = Substitute.For<ILogger>();

            searchDispatcher = new SearchDispatcher(searchServiceBusClient, searchService, blobStorageClient, logger);
        }

        [Test]
        public async Task DispatchSearch_DispatchesSearchWithId()
        {
            await searchDispatcher.DispatchSearch(new SearchRequest());

            await searchServiceBusClient.Received().PublishToSearchQueue(Arg.Is<IdentifiedSearchRequest>(r => r.Id != null));
        }

        [Test]
        public async Task RunSearch_RunsSearch()
        {
            var searchRequest = new SearchRequest();
            
            await searchDispatcher.RunSearch(new IdentifiedSearchRequest {SearchRequest = searchRequest});

            await searchService.Received().Search(searchRequest);
        }

        [Test]
        public async Task RunSearch_StoresResultsInBlobStorage()
        {
            const string id = "id";
            
            await searchDispatcher.RunSearch(new IdentifiedSearchRequest {Id = id});

            await blobStorageClient.Received().UploadResults(id, Arg.Any<IEnumerable<SearchResult>>());
        }

        [Test]
        public async Task RunSearch_PublishesSuccessNotification()
        {
            const string id = "id";
            
            await searchDispatcher.RunSearch(new IdentifiedSearchRequest {Id = id});

            await searchServiceBusClient.PublishToResultsNotificationTopic(Arg.Is<SearchResultsNotification>(r =>
                r.WasSuccessful && r.SearchRequestId == id
            ));
        }

        [Test]
        public async Task RunSearch_WhenSearchFails_PublishesFailureNotification()
        {
            const string id = "id";
            searchService.Search(Arg.Any<SearchRequest>()).Throws(new NovaHttpException(HttpStatusCode.InternalServerError, ""));
            
            await searchDispatcher.RunSearch(new IdentifiedSearchRequest {Id = id});

            await searchServiceBusClient.PublishToResultsNotificationTopic(Arg.Is<SearchResultsNotification>(r =>
                !r.WasSuccessful && r.SearchRequestId == id
            ));
        }
    }
}