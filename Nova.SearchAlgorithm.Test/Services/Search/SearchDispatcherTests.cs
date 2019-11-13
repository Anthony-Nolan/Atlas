using FluentValidation;
using Nova.SearchAlgorithm.Client.Models.SearchRequests;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Clients.AzureStorage;
using Nova.SearchAlgorithm.Clients.ServiceBus;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Services.ConfigurationProviders;
using Nova.SearchAlgorithm.Services.Search;
using Nova.SearchAlgorithm.Test.Builders;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.Http.Exceptions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using System.Net;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Test.Services.Search
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

            searchDispatcher = new SearchDispatcher(searchServiceBusClient, searchService, resultsBlobStorageClient, logger, wmdaHlaVersionProvider);
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
            
            await searchDispatcher.RunSearch(new IdentifiedSearchRequest {SearchRequest = searchRequest});

            await searchService.Received().Search(searchRequest);
        }

        [Test]
        public async Task RunSearch_StoresResultsInBlobStorage()
        {
            const string id = "id";
            
            await searchDispatcher.RunSearch(new IdentifiedSearchRequest {Id = id});

            await resultsBlobStorageClient.Received().UploadResults(id, Arg.Any<SearchResultSet>());
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
        public async Task RunSearch_PublishesWmdaVersionInNotification()
        {
            const string wmdaVersion = "wmda-version";
            wmdaHlaVersionProvider.GetActiveHlaDatabaseVersion().Returns(wmdaVersion);
            
            await searchDispatcher.RunSearch(new IdentifiedSearchRequest {Id = "id"});

            await searchServiceBusClient.PublishToResultsNotificationTopic(Arg.Is<SearchResultsNotification>(r =>
                r.WmdaHlaDatabaseVersion == wmdaVersion
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