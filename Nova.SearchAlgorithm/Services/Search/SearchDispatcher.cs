using System;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Client.Models.SearchRequests;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Clients;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Services.AzureStorage;
using Nova.Utils.ApplicationInsights;

namespace Nova.SearchAlgorithm.Services.Search
{
    public interface ISearchDispatcher
    {
        Task<string> DispatchSearch(SearchRequest searchRequest);
        Task RunSearch(IdentifiedSearchRequest identifiedSearchRequest);
    }

    public class SearchDispatcher : ISearchDispatcher
    {
        private readonly ISearchServiceBusClient searchServiceBusClient;
        private readonly ISearchService searchService;
        private readonly IBlobStorageClient blobStorageClient;
        private readonly ILogger logger;

        public SearchDispatcher(
            ISearchServiceBusClient searchServiceBusClient,
            ISearchService searchService,
            IBlobStorageClient blobStorageClient,
            ILogger logger)
        {
            this.searchServiceBusClient = searchServiceBusClient;
            this.searchService = searchService;
            this.blobStorageClient = blobStorageClient;
            this.logger = logger;
        }

        /// <returns>A unique identifier for the dispatched search request</returns>
        public async Task<string> DispatchSearch(SearchRequest searchRequest)
        {
            var searchId = Guid.NewGuid().ToString();
            var identifiedSearchRequest = new IdentifiedSearchRequest
            {
                SearchRequest = searchRequest,
                Id = searchId,
            };

            await searchServiceBusClient.PublishToSearchQueue(identifiedSearchRequest);
            return searchId;
        }

        public async Task RunSearch(IdentifiedSearchRequest identifiedSearchRequest)
        {
            var searchRequestId = identifiedSearchRequest.Id;
            try
            {
                var results = (await searchService.Search(identifiedSearchRequest.SearchRequest)).ToList();
                await blobStorageClient.UploadResults(searchRequestId, results);
                await searchServiceBusClient.PublishToResultsNotificationTopic(new SearchResultsNotification
                {
                    SearchRequestId = searchRequestId,
                    WasSuccessful = true,
                    NumberOfResults = results.Count
                });
            }
            catch (Exception e)
            {
                logger.SendTrace($"Failed to run search with id {searchRequestId}. Exception: {e}", LogLevel.Error);
                await searchServiceBusClient.PublishToResultsNotificationTopic(
                    new SearchResultsNotification {SearchRequestId = searchRequestId, WasSuccessful = false}
                );
            }
        }
    }
}