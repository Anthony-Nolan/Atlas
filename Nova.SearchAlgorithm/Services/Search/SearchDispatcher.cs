using FluentValidation;
using Nova.SearchAlgorithm.Client.Models.SearchRequests;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Clients.AzureStorage;
using Nova.SearchAlgorithm.Clients.ServiceBus;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Services.ConfigurationProviders;
using Nova.SearchAlgorithm.Validators.SearchRequest;
using Nova.Utils.ApplicationInsights;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

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
        private readonly IResultsBlobStorageClient resultsBlobStorageClient;
        private readonly ILogger logger;
        private readonly IWmdaHlaVersionProvider wmdaHlaVersionProvider;

        public SearchDispatcher(
            ISearchServiceBusClient searchServiceBusClient,
            ISearchService searchService,
            IResultsBlobStorageClient resultsBlobStorageClient,
            ILogger logger, 
            IWmdaHlaVersionProvider wmdaHlaVersionProvider)
        {
            this.searchServiceBusClient = searchServiceBusClient;
            this.searchService = searchService;
            this.resultsBlobStorageClient = resultsBlobStorageClient;
            this.logger = logger;
            this.wmdaHlaVersionProvider = wmdaHlaVersionProvider;
        }

        /// <returns>A unique identifier for the dispatched search request</returns>
        public async Task<string> DispatchSearch(SearchRequest searchRequest)
        {
            new SearchRequestValidator().ValidateAndThrow(searchRequest);
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
            var searchAlgorithmServiceVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var hlaDatabaseVersion = wmdaHlaVersionProvider.GetActiveHlaDatabaseVersion();

            try
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var results = (await searchService.Search(identifiedSearchRequest.SearchRequest)).ToList();
                stopwatch.Stop();
                
                await resultsBlobStorageClient.UploadResults(searchRequestId, new SearchResultSet
                {
                    SearchResults = results,
                    TotalResults = results.Count
                });
                var notification = new SearchResultsNotification
                {
                    SearchRequestId = searchRequestId,
                    SearchAlgorithmServiceVersion = searchAlgorithmServiceVersion,
                    WmdaHlaDatabaseVersion = hlaDatabaseVersion,
                    WasSuccessful = true,
                    NumberOfResults = results.Count,
                    BlobStorageContainerName = resultsBlobStorageClient.GetResultsContainerName(),
                    SearchTimeInMilliseconds = stopwatch.ElapsedMilliseconds
                };
                await searchServiceBusClient.PublishToResultsNotificationTopic(notification);
            }
            catch (Exception e)
            {
                logger.SendTrace($"Failed to run search with id {searchRequestId}. Exception: {e}", LogLevel.Error);
                var notification = new SearchResultsNotification
                {
                    WasSuccessful = false,
                    SearchRequestId = searchRequestId,
                    SearchAlgorithmServiceVersion = searchAlgorithmServiceVersion,
                    WmdaHlaDatabaseVersion = hlaDatabaseVersion
                };
                await searchServiceBusClient.PublishToResultsNotificationTopic(notification);
            }
        }
    }
}