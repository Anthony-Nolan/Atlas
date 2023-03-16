using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Clients.AzureStorage;
using Atlas.MatchingAlgorithm.Clients.ServiceBus;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Settings.Azure;
using Atlas.MatchingAlgorithm.Settings.ServiceBus;
using Atlas.MatchingAlgorithm.Validators.SearchRequest;
using FluentValidation;

namespace Atlas.MatchingAlgorithm.Services.Search
{
    public interface ISearchRunner
    {
        /// <param name="identifiedSearchRequest"></param>
        /// <param name="attemptNumber">The number of times this <paramref name="identifiedSearchRequest"/> has been attempted, including the current attempt.</param>
        Task RunSearch(IdentifiedSearchRequest identifiedSearchRequest, int attemptNumber);
    }

    public class SearchRunner : ISearchRunner
    {
        private readonly ISearchServiceBusClient searchServiceBusClient;
        private readonly ISearchService searchService;
        private readonly IResultsBlobStorageClient resultsBlobStorageClient;
        private readonly ILogger searchLogger;
        private readonly MatchingAlgorithmSearchLoggingContext searchLoggingContext;
        private readonly IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor;
        private readonly IMatchingFailureNotificationSender matchingFailureNotificationSender;
        private readonly bool resultBatched;
        private readonly int searchRequestMaxRetryCount;

        public SearchRunner(
            ISearchServiceBusClient searchServiceBusClient,
            ISearchService searchService,
            IResultsBlobStorageClient resultsBlobStorageClient,
            // ReSharper disable once SuggestBaseTypeForParameter
            IMatchingAlgorithmSearchLogger searchLogger,
            MatchingAlgorithmSearchLoggingContext searchLoggingContext,
            IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor,
            MessagingServiceBusSettings messagingServiceBusSettings,
            IMatchingFailureNotificationSender matchingFailureNotificationSender,
            AzureStorageSettings azureStorageSettings)
        {
            this.searchServiceBusClient = searchServiceBusClient;
            this.searchService = searchService;
            this.resultsBlobStorageClient = resultsBlobStorageClient;
            this.searchLogger = searchLogger;
            this.searchLoggingContext = searchLoggingContext;
            this.hlaNomenclatureVersionAccessor = hlaNomenclatureVersionAccessor;
            this.matchingFailureNotificationSender = matchingFailureNotificationSender;
            searchRequestMaxRetryCount = messagingServiceBusSettings.SearchRequestsMaxDeliveryCount;
            resultBatched = azureStorageSettings.ResultBatched;
        }

        public async Task RunSearch(IdentifiedSearchRequest identifiedSearchRequest, int attemptNumber)
        {
            var searchRequestId = identifiedSearchRequest.Id;
            searchLoggingContext.SearchRequestId = searchRequestId;
            var hlaNomenclatureVersion = hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion();
            searchLoggingContext.HlaNomenclatureVersion = hlaNomenclatureVersion;

            try
            {
                await new SearchRequestValidator().ValidateAndThrowAsync(identifiedSearchRequest.SearchRequest);

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var results = (await searchService.Search(identifiedSearchRequest.SearchRequest, null)).ToList();
                stopwatch.Stop();

                var blobContainerName = resultsBlobStorageClient.GetResultsContainerName();

                var searchResultSet = new OriginalMatchingAlgorithmResultSet
                {
                    SearchRequestId = searchRequestId,
                    Results = results,
                    TotalResults = results.Count,
                    MatchingAlgorithmHlaNomenclatureVersion = hlaNomenclatureVersion,
                    BlobStorageContainerName = blobContainerName,
                    SearchRequest = identifiedSearchRequest.SearchRequest
                };

                await resultsBlobStorageClient.UploadResults(searchResultSet);

                var notification = new MatchingResultsNotification
                {
                    SearchRequest = identifiedSearchRequest.SearchRequest,
                    SearchRequestId = searchRequestId,
                    MatchingAlgorithmServiceVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
                    MatchingAlgorithmHlaNomenclatureVersion = hlaNomenclatureVersion,
                    WasSuccessful = true,
                    NumberOfResults = results.Count,
                    BlobStorageContainerName = blobContainerName,
                    ResultsFileName = searchResultSet.ResultsFileName,
                    ElapsedTime = stopwatch.Elapsed,
                    ResultBatched = resultBatched,
                    BatchFolder = resultBatched && results.Any() ? searchRequestId : null
                };
                await searchServiceBusClient.PublishToResultsNotificationTopic(notification);
            }
            // Invalid HLA is treated as an "Expected error" pathway and will not be retried.
            // This means only a single failure notification will be sent out, and the request message will be completed and not dead-lettered.
            catch (HlaMetadataDictionaryException hmdException)
            {
                searchLogger.SendTrace($"Failed to lookup HLA for search with id {searchRequestId}. Exception: {hmdException}", LogLevel.Error);
                
                await matchingFailureNotificationSender.SendFailureNotification(searchRequestId, attemptNumber, 0, hmdException.Message);
                
                // Do not re-throw the HMD exception to prevent the search being retried or dead-lettered.
            }
            // "Unexpected" exceptions will be re-thrown to ensure that the request will be retried or dead-lettered, as appropriate.
            catch (Exception e)
            {
                searchLogger.SendTrace($"Failed to run search with id {searchRequestId}. Exception: {e}", LogLevel.Error);

                await matchingFailureNotificationSender.SendFailureNotification(searchRequestId, attemptNumber, searchRequestMaxRetryCount - attemptNumber);

                throw;
            }
        }
    }
}