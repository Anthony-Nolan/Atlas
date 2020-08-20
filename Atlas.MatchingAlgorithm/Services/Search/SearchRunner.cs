using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Common.ApplicationInsights;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Clients.AzureStorage;
using Atlas.MatchingAlgorithm.Clients.ServiceBus;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Validators.SearchRequest;
using FluentValidation;

namespace Atlas.MatchingAlgorithm.Services.Search
{
    public interface ISearchRunner
    {
        Task<MatchingAlgorithmResultSet> RunSearch(IdentifiedSearchRequest identifiedSearchRequest);
    }

    public class SearchRunner : ISearchRunner
    {
        private readonly ISearchServiceBusClient searchServiceBusClient;
        private readonly ISearchService searchService;
        private readonly IResultsBlobStorageClient resultsBlobStorageClient;
        private readonly ILogger searchLogger;
        private readonly MatchingAlgorithmSearchLoggingContext searchLoggingContext;
        private readonly IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor;

        public SearchRunner(
            ISearchServiceBusClient searchServiceBusClient,
            ISearchService searchService,
            IResultsBlobStorageClient resultsBlobStorageClient,
            // ReSharper disable once SuggestBaseTypeForParameter
            IMatchingAlgorithmSearchLogger searchLogger,
            MatchingAlgorithmSearchLoggingContext searchLoggingContext,
            IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor)
        {
            this.searchServiceBusClient = searchServiceBusClient;
            this.searchService = searchService;
            this.resultsBlobStorageClient = resultsBlobStorageClient;
            this.searchLogger = searchLogger;
            this.searchLoggingContext = searchLoggingContext;
            this.hlaNomenclatureVersionAccessor = hlaNomenclatureVersionAccessor;
        }

        public async Task<MatchingAlgorithmResultSet> RunSearch(IdentifiedSearchRequest identifiedSearchRequest)
        {
            await new SearchRequestValidator().ValidateAndThrowAsync(identifiedSearchRequest.SearchRequest);
            
            var searchRequestId = identifiedSearchRequest.Id;
            searchLoggingContext.SearchRequestId = searchRequestId;
            var searchAlgorithmServiceVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
            var hlaNomenclatureVersion = hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion();
            searchLoggingContext.HlaNomenclatureVersion = hlaNomenclatureVersion;

            try
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var results = (await searchService.Search(identifiedSearchRequest.SearchRequest)).ToList();
                stopwatch.Stop();

                var blobContainerName = resultsBlobStorageClient.GetResultsContainerName();

                var searchResultSet = new MatchingAlgorithmResultSet
                {
                    SearchRequestId = searchRequestId,
                    MatchingAlgorithmResults = results,
                    ResultCount = results.Count,
                    HlaNomenclatureVersion = hlaNomenclatureVersion,
                    BlobStorageContainerName = blobContainerName,
                };

                await resultsBlobStorageClient.UploadResults(searchResultSet);

                var notification = new MatchingResultsNotification
                {
                    SearchRequest = identifiedSearchRequest.SearchRequest,
                    SearchRequestId = searchRequestId,
                    SearchAlgorithmServiceVersion = searchAlgorithmServiceVersion,
                    HlaNomenclatureVersion = hlaNomenclatureVersion,
                    WasSuccessful = true,
                    NumberOfResults = results.Count,
                    BlobStorageContainerName = blobContainerName,
                    BlobStorageResultsFileName = searchResultSet.ResultsFileName,
                    ElapsedTime = stopwatch.Elapsed
                };
                await searchServiceBusClient.PublishToResultsNotificationTopic(notification);
                return searchResultSet;
            }
            catch (Exception e)
            {
                searchLogger.SendTrace($"Failed to run search with id {searchRequestId}. Exception: {e}", LogLevel.Error);
                var notification = new MatchingResultsNotification
                {
                    WasSuccessful = false,
                    SearchRequestId = searchRequestId,
                    SearchAlgorithmServiceVersion = searchAlgorithmServiceVersion,
                    HlaNomenclatureVersion = hlaNomenclatureVersion
                };
                await searchServiceBusClient.PublishToResultsNotificationTopic(notification);
                throw;
            }
        }
    }
}