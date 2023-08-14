using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.LogFile;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.Blob;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
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
        Task RunSearch(IdentifiedSearchRequest identifiedSearchRequest, int attemptNumber, DateTimeOffset enqueuedTimeUtc);
    }

    public class SearchRunner : ISearchRunner
    {
        private readonly ISearchServiceBusClient searchServiceBusClient;
        private readonly ISearchService searchService;
        private readonly ISearchResultsBlobStorageClient resultsBlobStorageClient;
        private readonly ILogger searchLogger;
        private readonly MatchingAlgorithmSearchLoggingContext searchLoggingContext;
        private readonly IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor;
        private readonly IMatchingFailureNotificationSender matchingFailureNotificationSender;
        private readonly int searchRequestMaxRetryCount;
        private readonly AzureStorageSettings azureStorageSettings;

        public SearchRunner(
            ISearchServiceBusClient searchServiceBusClient,
            ISearchService searchService,
            ISearchResultsBlobStorageClient resultsBlobStorageClient,
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
            this.azureStorageSettings = azureStorageSettings;
        }

        public async Task RunSearch(IdentifiedSearchRequest identifiedSearchRequest, int attemptNumber, DateTimeOffset enqueuedTimeUtc)
        {
            var searchStartTime = DateTimeOffset.UtcNow;
            var searchRequestId = identifiedSearchRequest.Id;
            searchLoggingContext.SearchRequestId = searchRequestId;
            var hlaNomenclatureVersion = hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion();
            searchLoggingContext.HlaNomenclatureVersion = hlaNomenclatureVersion;
            var requestCompletedSuccessfully = false;
            var searchStopWatch = new Stopwatch();

            try
            {
                await new SearchRequestValidator().ValidateAndThrowAsync(identifiedSearchRequest.SearchRequest);

                searchStopWatch.Start();
                var results = (await searchService.Search(identifiedSearchRequest.SearchRequest, null)).ToList();
                searchStopWatch.Stop();

                var searchResultSet = new OriginalMatchingAlgorithmResultSet
                {
                    SearchRequestId = searchRequestId,
                    Results = results,
                    TotalResults = results.Count,
                    MatchingAlgorithmHlaNomenclatureVersion = hlaNomenclatureVersion,
                    BlobStorageContainerName = azureStorageSettings.SearchResultsBlobContainer,
                    SearchRequest = identifiedSearchRequest.SearchRequest,
                    BatchedResult = azureStorageSettings.ShouldBatchResults,
                    MatchingStartTime = searchStartTime
                };

                await resultsBlobStorageClient.UploadResults(searchResultSet, azureStorageSettings.SearchResultsBatchSize,
                    searchResultSet.SearchRequestId);

                var notification = new MatchingResultsNotification
                {
                    SearchRequest = identifiedSearchRequest.SearchRequest,
                    SearchRequestId = searchRequestId,
                    MatchingAlgorithmServiceVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
                    MatchingAlgorithmHlaNomenclatureVersion = hlaNomenclatureVersion,
                    WasSuccessful = true,
                    NumberOfResults = results.Count,
                    BlobStorageContainerName = azureStorageSettings.SearchResultsBlobContainer,
                    ResultsFileName = searchResultSet.ResultsFileName,
                    ElapsedTime = searchStopWatch.Elapsed,
                    ResultsBatched = azureStorageSettings.ShouldBatchResults,
                    BatchFolderName = azureStorageSettings.ShouldBatchResults && results.Any() ? searchRequestId : null
                };
                await searchServiceBusClient.PublishToResultsNotificationTopic(notification);

                requestCompletedSuccessfully = true;
            }
            // Invalid HLA is treated as an "Expected error" pathway and will not be retried.
            // This means only a single failure notification will be sent out, and the request message will be completed and not dead-lettered.
            catch (HlaMetadataDictionaryException hmdException)
            {
                searchLogger.SendTrace($"Failed to lookup HLA for search with id {searchRequestId}. Exception: {hmdException}", LogLevel.Error);

                await matchingFailureNotificationSender.SendFailureNotification(identifiedSearchRequest, attemptNumber, 0, hmdException.Message);

                // Do not re-throw the HMD exception to prevent the search being retried or dead-lettered.
            }
            // "Unexpected" exceptions will be re-thrown to ensure that the request will be retried or dead-lettered, as appropriate.
            catch (Exception e)
            {
                searchLogger.SendTrace($"Failed to run search with id {searchRequestId}. Exception: {e}", LogLevel.Error);

                await matchingFailureNotificationSender.SendFailureNotification(identifiedSearchRequest, attemptNumber,
                    searchRequestMaxRetryCount - attemptNumber);

                throw;
            }
            finally
            {
                await UploadSearchLog(new MatchingSearchLog
                {
                    SearchRequestId = searchRequestId,
                    WasSuccessful = requestCompletedSuccessfully,
                    AttemptNumber = attemptNumber,
                    SearchRequest = identifiedSearchRequest.SearchRequest,
                    RequestPerformanceMetrics = new RequestPerformanceMetrics
                    {
                        InitiationTime = enqueuedTimeUtc,
                        StartTime = searchStartTime,
                        CompletionTime = DateTimeOffset.UtcNow,
                        AlgorithmCoreStepDuration = searchStopWatch.Elapsed
                    }
                });
            }
        }

        public async Task UploadSearchLog(MatchingSearchLog searchLog)
        {
            try
            {
                await resultsBlobStorageClient.UploadResults(searchLog, azureStorageSettings.SearchResultsBlobContainer, $"{searchLog.SearchRequestId}-log.json");
            }
            catch
            {
                searchLogger.SendTrace($"Failed to write performance log file for search with id {searchLog.SearchRequestId}.", LogLevel.Error);
            }
        }
    }
}