using Atlas.MatchingAlgorithm.ApplicationInsights.SearchRequests;
using Atlas.MatchingAlgorithm.Client.Models.SearchResults;
using Atlas.MatchingAlgorithm.Clients.AzureStorage;
using Atlas.MatchingAlgorithm.Clients.ServiceBus;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;

namespace Atlas.MatchingAlgorithm.Services.Search
{
    public interface ISearchRunner
    {
        Task RunSearch(IdentifiedSearchRequest identifiedSearchRequest);
    }

    public class SearchRunner : ISearchRunner
    {
        private readonly ISearchServiceBusClient searchServiceBusClient;
        private readonly ISearchService searchService;
        private readonly IResultsBlobStorageClient resultsBlobStorageClient;
        private readonly ILogger logger;
        private readonly ISearchRequestContext searchRequestContext;
        private readonly IActiveHlaVersionAccessor hlaVersionProvider;

        public SearchRunner(
            ISearchServiceBusClient searchServiceBusClient,
            ISearchService searchService,
            IResultsBlobStorageClient resultsBlobStorageClient,
            ILogger logger,
            ISearchRequestContext searchRequestContext,
            IActiveHlaVersionAccessor hlaVersionProvider)
        {
            this.searchServiceBusClient = searchServiceBusClient;
            this.searchService = searchService;
            this.resultsBlobStorageClient = resultsBlobStorageClient;
            this.logger = logger;
            this.searchRequestContext = searchRequestContext;
            this.hlaVersionProvider = hlaVersionProvider;
        }

        public async Task RunSearch(IdentifiedSearchRequest identifiedSearchRequest)
        {
            var searchRequestId = identifiedSearchRequest.Id;
            searchRequestContext.SearchRequestId = searchRequestId;
            var searchAlgorithmServiceVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var hlaDatabaseVersion = hlaVersionProvider.GetActiveHlaDatabaseVersion();

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