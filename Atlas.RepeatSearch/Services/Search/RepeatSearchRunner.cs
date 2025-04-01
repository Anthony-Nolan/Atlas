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
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.RepeatSearch.Clients;
using Atlas.RepeatSearch.Data.Models;
using Atlas.RepeatSearch.Data.Repositories;
using Atlas.RepeatSearch.Models;
using Atlas.RepeatSearch.Services.ResultSetTracking;
using Atlas.RepeatSearch.Settings.Azure;
using Atlas.RepeatSearch.Settings.ServiceBus;
using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Common.Models;
using MatchingAlgorithmFailureInfo = Atlas.SearchTracking.Common.Models.MatchingAlgorithmFailureInfo;

namespace Atlas.RepeatSearch.Services.Search
{
    public interface IRepeatSearchRunner
    {
        Task RunSearch(IdentifiedRepeatSearchRequest identifiedRepeatSearchRequest, int attemptNumber, DateTimeOffset enqueuedTimeUtc);
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
        private readonly IMatchingAlgorithmSearchTrackingContextManager matchingAlgorithmSearchTrackingContextManager;
        private readonly IMatchingAlgorithmSearchTrackingDispatcher matchingAlgorithmSearchTrackingDispatcher;

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
            IRepeatSearchMatchingFailureNotificationSender repeatSearchMatchingFailureNotificationSender,
            IMatchingAlgorithmSearchTrackingContextManager matchingAlgorithmSearchTrackingContextManager,
            IMatchingAlgorithmSearchTrackingDispatcher matchingAlgorithmSearchTrackingDispatcher)
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
            this.matchingAlgorithmSearchTrackingContextManager = matchingAlgorithmSearchTrackingContextManager;
            this.matchingAlgorithmSearchTrackingDispatcher = matchingAlgorithmSearchTrackingDispatcher;
        }

        public async Task RunSearch(IdentifiedRepeatSearchRequest identifiedRepeatSearchRequest, int attemptNumber, DateTimeOffset enqueuedTimeUtc)
        {
            var searchStartTime = DateTimeOffset.UtcNow;
            var searchRequestId = identifiedRepeatSearchRequest.OriginalSearchId;
            var repeatSearchId = identifiedRepeatSearchRequest.RepeatSearchId;
            var searchAlgorithmServiceVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
            var hlaNomenclatureVersion = hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion();
            var diff = new SearchResultDifferential();
            var resultsSentTime = new DateTime();
            var requestCompletedSuccessfully = false;
            var numberOfResults = 0;
            MatchingAlgorithmFailureInfo matchingAlgorithmFailureInfo = null;

            repeatSearchLoggingContext.SearchRequestId = searchRequestId;
            repeatSearchLoggingContext.HlaNomenclatureVersion = hlaNomenclatureVersion;

            try
            {
                await repeatSearchValidator.ValidateRepeatSearchAndThrow(identifiedRepeatSearchRequest.RepeatSearchRequest);

                var context = new MatchingAlgorithmSearchTrackingContext
                {
                    SearchRequestId = new Guid(searchRequestId),
                    AttemptNumber = (byte)attemptNumber
                };

                matchingAlgorithmSearchTrackingContextManager.Set(context);
                await matchingAlgorithmSearchTrackingDispatcher.ProcessInitiation(enqueuedTimeUtc.UtcDateTime, searchStartTime.UtcDateTime);

                // ReSharper disable once PossibleInvalidOperationException - validation has ensured this is not null.
                var searchCutoffDate = identifiedRepeatSearchRequest.RepeatSearchRequest.SearchCutoffDate.Value;

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var results = (await searchService.Search(identifiedRepeatSearchRequest.RepeatSearchRequest.SearchRequest, searchCutoffDate))
                    .ToList();
                numberOfResults = results.Count;

                diff = await CalculateAndStoreResultsDiff(searchRequestId, results, searchCutoffDate);

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

                await matchingAlgorithmSearchTrackingDispatcher.ProcessPersistingResultsStarted();
                await repeatResultsBlobStorageClient.UploadResults(searchResultSet, azureStorageSettings.SearchResultsBatchSize,
                    $"{searchRequestId}/{repeatSearchId}");
                await matchingAlgorithmSearchTrackingDispatcher.ProcessPersistingResultsEnded();
                resultsSentTime = DateTime.UtcNow;

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

                requestCompletedSuccessfully = true;
            }

            #region Expected Exceptions

            // Invalid requests are treated as an "Expected error" pathways.
            // They are not be re-thrown to prevent retries.
            // Only a single failure notification is sent out and the request message will be completed, not dead-lettered.

            catch (FluentValidation.ValidationException validationException)
            {
                await HandleValidationExceptionWithoutRethrow(validationException);

                matchingAlgorithmFailureInfo = new MatchingAlgorithmFailureInfo
                {
                    Type = MatchingAlgorithmFailureType.ValidationError,
                    Message = validationException.Message,
                    ExceptionStacktrace = validationException.ToString()
                };
            }
            catch (HlaMetadataDictionaryException hmdException)
            {
                await HandleValidationExceptionWithoutRethrow(hmdException);

                matchingAlgorithmFailureInfo = new MatchingAlgorithmFailureInfo
                {
                    Type = MatchingAlgorithmFailureType.HlaMetadataDictionaryError,
                    Message = hmdException.Message,
                    ExceptionStacktrace = hmdException.ToString()
                };
            }

            #endregion

            // "Unexpected" exceptions will be re-thrown to ensure that the request will be retried or dead-lettered, as appropriate.
            catch (Exception e)
            {
                repeatSearchLogger.SendTrace(
                    $"Failed to run search with repeat search id: {repeatSearchId} (search id: {searchRequestId}). Exception: {e}",
                    LogLevel.Error);
                await repeatSearchMatchingFailureNotificationSender.SendFailureNotification(identifiedRepeatSearchRequest, attemptNumber,
                    searchRequestMaxRetryCount - attemptNumber);

                matchingAlgorithmFailureInfo = new MatchingAlgorithmFailureInfo
                {
                    Type = MatchingAlgorithmFailureType.UnexpectedError,
                    Message = e.Message,
                    ExceptionStacktrace = e.ToString()
                };

                throw;
            }
            finally
            {
                var matchingAlgorithmCompletedEvent = new MatchingAlgorithmCompletedEvent
                {
                    SearchRequestId = new Guid(searchRequestId),
                    AttemptNumber = (byte)attemptNumber,
                    CompletionTimeUtc = DateTime.UtcNow,
                    HlaNomenclatureVersion = hlaNomenclatureVersion,
                    ResultsSent = requestCompletedSuccessfully,
                    ResultsSentTimeUtc = resultsSentTime,
                    CompletionDetails = new MatchingAlgorithmCompletionDetails
                    {
                        IsSuccessful = requestCompletedSuccessfully,
                        TotalAttemptsNumber = (byte)attemptNumber,
                        NumberOfResults = numberOfResults,
                        NumberOfMatching = diff.NewResults.Count,
                        RepeatSearchResultsDetails = new MatchingAlgorithmRepeatSearchResultsDetails
                        {
                            AddedResultCount = diff.NewResults.Count,
                            RemovedResultCount = diff.RemovedResults.Count,
                            UpdatedResultCount = diff.UpdatedResults.Count
                        },
                        FailureInfo = matchingAlgorithmFailureInfo
                    }
                };

                await matchingAlgorithmSearchTrackingDispatcher.ProcessCompleted(matchingAlgorithmCompletedEvent);
            }

            async Task HandleValidationExceptionWithoutRethrow(Exception ex)
            {
                repeatSearchLogger.SendTrace(
                    $"Validation failed for repeat search id: {repeatSearchId} (search id: {searchRequestId}). Exception: {ex}",
                    LogLevel.Error);

                await repeatSearchMatchingFailureNotificationSender.SendFailureNotification(
                    identifiedRepeatSearchRequest, attemptNumber, 0, ex.Message);
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