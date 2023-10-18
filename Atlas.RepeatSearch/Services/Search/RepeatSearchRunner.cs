using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.AzureStorage.Blob;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.RepeatSearch.Clients;
using Atlas.RepeatSearch.Data.Models;
using Atlas.RepeatSearch.Data.Repositories;
using Atlas.RepeatSearch.Models;
using Atlas.RepeatSearch.Services.ResultSetTracking;
using Atlas.RepeatSearch.Settings.Azure;
using Atlas.RepeatSearch.Settings.ServiceBus;

namespace Atlas.RepeatSearch.Services.Search
{
    public interface IRepeatSearchRunner
    {
        Task RunSearch(IdentifiedRepeatSearchRequest identifiedRepeatSearchRequest, int attemptNumber);
    }

    public class RepeatSearchRunner : IRepeatSearchRunner
    {
        private readonly IRepeatSearchServiceBusClient repeatSearchServiceBusClient;
        private readonly ISearchService searchService;
        private readonly ISearchResultsBlobStorageClient repeatResultsBlobStorageClient;
        private readonly ILogger repeatSearchLogger;
        private readonly MatchingAlgorithmSearchLoggingContext repeatSearchLoggingContext;
        private readonly IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor;
        private readonly IRepeatSearchHistoryRepository repeatSearchHistoryRepository;
        private readonly IRepeatSearchValidator repeatSearchValidator;
        private readonly IRepeatSearchDifferentialCalculator repeatSearchDifferentialCalculator;
        private readonly IOriginalSearchResultSetTracker originalSearchResultSetTracker;
        private readonly AzureStorageSettings azureStorageSettings;
        private readonly int searchRequestMaxRetryCount;
        private readonly IRepeatSearchMatchingFailureNotificationSender repeatSearchMatchingFailureNotificationSender;

        public RepeatSearchRunner(
            IRepeatSearchServiceBusClient repeatSearchServiceBusClient,
            ISearchService searchService,
            ISearchResultsBlobStorageClient repeatResultsBlobStorageClient,
            // ReSharper disable once SuggestBaseTypeForParameter
            IMatchingAlgorithmSearchLogger repeatSearchLogger,
            MatchingAlgorithmSearchLoggingContext repeatSearchLoggingContext,
            IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor,
            IRepeatSearchHistoryRepository repeatSearchHistoryRepository,
            IRepeatSearchValidator repeatSearchValidator,
            IRepeatSearchDifferentialCalculator repeatSearchDifferentialCalculator,
            IOriginalSearchResultSetTracker originalSearchResultSetTracker,
            AzureStorageSettings azureStorageSettings,
            MessagingServiceBusSettings messagingServiceBusSettings,
            IRepeatSearchMatchingFailureNotificationSender repeatSearchMatchingFailureNotificationSender)
        {
            this.repeatSearchServiceBusClient = repeatSearchServiceBusClient;
            this.searchService = searchService;
            this.repeatResultsBlobStorageClient = repeatResultsBlobStorageClient;
            this.repeatSearchLogger = repeatSearchLogger;
            this.repeatSearchLoggingContext = repeatSearchLoggingContext;
            this.hlaNomenclatureVersionAccessor = hlaNomenclatureVersionAccessor;
            this.repeatSearchHistoryRepository = repeatSearchHistoryRepository;
            this.repeatSearchValidator = repeatSearchValidator;
            this.repeatSearchDifferentialCalculator = repeatSearchDifferentialCalculator;
            this.originalSearchResultSetTracker = originalSearchResultSetTracker;
            this.azureStorageSettings = azureStorageSettings;
            searchRequestMaxRetryCount = messagingServiceBusSettings.RepeatSearchRequestsMaxDeliveryCount;
            this.repeatSearchMatchingFailureNotificationSender = repeatSearchMatchingFailureNotificationSender;
        }

        public async Task RunSearch(IdentifiedRepeatSearchRequest identifiedRepeatSearchRequest, int attemptNumber)
        {
            var searchStartTime = DateTimeOffset.UtcNow;
            await repeatSearchValidator.ValidateRepeatSearchAndThrow(identifiedRepeatSearchRequest.RepeatSearchRequest);

            var searchRequestId = identifiedRepeatSearchRequest.OriginalSearchId;
            var repeatSearchId = identifiedRepeatSearchRequest.RepeatSearchId;

            repeatSearchLoggingContext.SearchRequestId = searchRequestId;
            var searchAlgorithmServiceVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
            var hlaNomenclatureVersion = hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion();
            repeatSearchLoggingContext.HlaNomenclatureVersion = hlaNomenclatureVersion;

            try
            {
                // ReSharper disable once PossibleInvalidOperationException - validation has ensured this is not null.
                var searchCutoffDate = identifiedRepeatSearchRequest.RepeatSearchRequest.SearchCutoffDate.Value;

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var results = (await searchService.Search(identifiedRepeatSearchRequest.RepeatSearchRequest.SearchRequest, searchCutoffDate))
                    .ToList();

                var diff = await CalculateAndStoreResultsDiff(searchRequestId, results, searchCutoffDate);

                await RecordRepeatSearch(identifiedRepeatSearchRequest, diff);

                stopwatch.Stop();

                var searchResultSet = new RepeatMatchingAlgorithmResultSet
                {
                    SearchRequest = identifiedRepeatSearchRequest.RepeatSearchRequest.SearchRequest,
                    SearchRequestId = searchRequestId,
                    RepeatSearchId = repeatSearchId,
                    Results = results,
                    TotalResults = results.Count,
                    MatchingAlgorithmHlaNomenclatureVersion = hlaNomenclatureVersion,
                    BlobStorageContainerName = azureStorageSettings.MatchingResultsBlobContainer,
                    NoLongerMatchingDonors = diff.RemovedResults.ToList(),
                    BatchedResult = azureStorageSettings.ShouldBatchResults,
                    MatchingStartTime = searchStartTime
                };

                await repeatResultsBlobStorageClient.UploadResults(searchResultSet, azureStorageSettings.SearchResultsBatchSize, $"{searchRequestId}/{repeatSearchId}");

                var notification = new MatchingResultsNotification
                {
                    SearchRequest = identifiedRepeatSearchRequest.RepeatSearchRequest.SearchRequest,
                    RepeatSearchRequestId = identifiedRepeatSearchRequest.RepeatSearchId,
                    SearchRequestId = searchRequestId,
                    MatchingAlgorithmServiceVersion = searchAlgorithmServiceVersion,
                    MatchingAlgorithmHlaNomenclatureVersion = hlaNomenclatureVersion,
                    WasSuccessful = true,
                    NumberOfResults = results.Count,
                    BlobStorageContainerName = azureStorageSettings.MatchingResultsBlobContainer,
                    ResultsFileName = searchResultSet.ResultsFileName,
                    ElapsedTime = stopwatch.Elapsed,
                    ResultsBatched = azureStorageSettings.ShouldBatchResults,
                    BatchFolderName = azureStorageSettings.ShouldBatchResults && results.Any() ? $"{searchRequestId}/{repeatSearchId}" : null
                };
                await repeatSearchServiceBusClient.PublishToResultsNotificationTopic(notification);
            }
            // Invalid HLA is treated as an "Expected error" pathway and will not be retried.
            // This means only a single failure notification will be sent out, and the request message will be completed and not dead-lettered.
            catch (HlaMetadataDictionaryException hmdException)
            {
                repeatSearchLogger.SendTrace($"Failed to lookup HLA for search with id {searchRequestId}. Exception: {hmdException}", LogLevel.Error);

                await repeatSearchMatchingFailureNotificationSender.SendFailureNotification(identifiedRepeatSearchRequest, attemptNumber, 0, hmdException.Message);

                // Do not re-throw the HMD exception to prevent the search being retried or dead-lettered.
            }
            // "Unexpected" exceptions will be re-thrown to ensure that the request will be retried or dead-lettered, as appropriate.
            catch (Exception e)
            {
                repeatSearchLogger.SendTrace($"Failed to run search with id {searchRequestId}. Exception: {e}", LogLevel.Error);
                await repeatSearchMatchingFailureNotificationSender.SendFailureNotification(identifiedRepeatSearchRequest, attemptNumber, searchRequestMaxRetryCount - attemptNumber);
                throw;
            }
        }

        private async Task<SearchResultDifferential> CalculateAndStoreResultsDiff(
            string searchRequestId,
            List<MatchingAlgorithmResult> results,
            DateTimeOffset searchCutoffDate)
        {
            using (repeatSearchLogger.RunTimed("Calculate and apply result diff to canonical result set"))
            {
                var diff = await repeatSearchDifferentialCalculator.CalculateDifferential(searchRequestId, results, searchCutoffDate);
                await originalSearchResultSetTracker.ApplySearchResultDiff(searchRequestId, diff);

                repeatSearchLogger.SendTrace(
                    $"Donor Result Diff Calculated. {diff.NewResults.Count} new results. {diff.UpdatedResults.Count} updated results. {diff.RemovedResults.Count} removed results."
                );

                return diff;
            }
        }

        private async Task RecordRepeatSearch(
            IdentifiedRepeatSearchRequest identifiedRepeatSearchRequest,
            SearchResultDifferential searchResultDifferential)
        {
            var historyRecord = new RepeatSearchHistoryRecord
            {
                DateCreated = DateTimeOffset.UtcNow,
                // ReSharper disable once PossibleInvalidOperationException - validation should have caught nulls by now
                SearchCutoffDate = identifiedRepeatSearchRequest.RepeatSearchRequest.SearchCutoffDate.Value,
                OriginalSearchRequestId = identifiedRepeatSearchRequest.OriginalSearchId,
                RepeatSearchRequestId = identifiedRepeatSearchRequest.RepeatSearchId,
                AddedResultCount = searchResultDifferential.NewResults.Count,
                UpdatedResultCount = searchResultDifferential.UpdatedResults.Count,
                RemovedResultCount = searchResultDifferential.RemovedResults.Count,
            };

            await repeatSearchHistoryRepository.RecordRepeatSearchRequest(historyRecord);
        }
    }
}