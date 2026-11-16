using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
using Atlas.RepeatSearch.Settings;
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
        private readonly IAtlasLogger repeatSearchLogger;
        private readonly MatchingAlgorithmSearchLoggingContext repeatSearchLoggingContext;
        private readonly IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor;
        private readonly IActiveDataRefreshRecordAccessor activeDataRefreshRecordAccessor;
        private readonly IRepeatSearchHistoryRepository repeatSearchHistoryRepository;
        private readonly IRepeatSearchValidator repeatSearchValidator;
        private readonly IRepeatSearchDifferentialCalculator repeatSearchDifferentialCalculator;
        private readonly IOriginalSearchResultSetTracker originalSearchResultSetTracker;
        private readonly AzureStorageSettings azureStorageSettings;
        private readonly int searchRequestMaxRetryCount;
        private readonly IRepeatSearchMatchingFailureNotificationSender repeatSearchMatchingFailureNotificationSender;
        private readonly MatchingAlgorithmSearchTrackingContext matchingAlgorithmSearchTrackingContext;
        private readonly ISearchTrackingEventPublisher searchTrackingEventPublisher;
        private readonly TimeSpan originalResultsWaitInterval;
        private readonly TimeSpan maxWaitForOriginalResults;

        public RepeatSearchRunner(
            IRepeatSearchServiceBusClient repeatSearchServiceBusClient,
            ISearchService searchService,
            ISearchResultsBlobStorageClient repeatResultsBlobStorageClient,
            IMatchingAlgorithmSearchLogger repeatSearchLogger,
            MatchingAlgorithmSearchLoggingContext repeatSearchLoggingContext,
            IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor,
            IActiveDataRefreshRecordAccessor activeDataRefreshRecordAccessor,
            IRepeatSearchHistoryRepository repeatSearchHistoryRepository,
            IRepeatSearchValidator repeatSearchValidator,
            IRepeatSearchDifferentialCalculator repeatSearchDifferentialCalculator,
            IOriginalSearchResultSetTracker originalSearchResultSetTracker,
            AzureStorageSettings azureStorageSettings,
            MessagingServiceBusSettings messagingServiceBusSettings,
            IRepeatSearchMatchingFailureNotificationSender repeatSearchMatchingFailureNotificationSender,
            MatchingAlgorithmSearchTrackingContext matchingAlgorithmSearchTrackingContext,
            ISearchTrackingEventPublisher searchTrackingEventPublisher,
            RepeatSearchSettings repeatSearchSettings)
        {
            this.repeatSearchServiceBusClient = repeatSearchServiceBusClient;
            this.searchService = searchService;
            this.repeatResultsBlobStorageClient = repeatResultsBlobStorageClient;
            this.repeatSearchLogger = repeatSearchLogger;
            this.repeatSearchLoggingContext = repeatSearchLoggingContext;
            this.hlaNomenclatureVersionAccessor = hlaNomenclatureVersionAccessor;
            this.activeDataRefreshRecordAccessor = activeDataRefreshRecordAccessor;
            this.repeatSearchHistoryRepository = repeatSearchHistoryRepository;
            this.repeatSearchValidator = repeatSearchValidator;
            this.repeatSearchDifferentialCalculator = repeatSearchDifferentialCalculator;
            this.originalSearchResultSetTracker = originalSearchResultSetTracker;
            this.azureStorageSettings = azureStorageSettings;
            searchRequestMaxRetryCount = messagingServiceBusSettings.RepeatSearchRequestsMaxDeliveryCount;
            this.repeatSearchMatchingFailureNotificationSender = repeatSearchMatchingFailureNotificationSender;
            this.matchingAlgorithmSearchTrackingContext = matchingAlgorithmSearchTrackingContext;
            this.searchTrackingEventPublisher = searchTrackingEventPublisher;
            originalResultsWaitInterval = TimeSpan.FromSeconds(repeatSearchSettings.OriginalResultsWaitIntervalSeconds);
            maxWaitForOriginalResults = TimeSpan.FromMinutes(repeatSearchSettings.MaxWaitForOriginalResultsMinutes);
        }

        public async Task RunSearch(IdentifiedRepeatSearchRequest identifiedRepeatSearchRequest, int attemptNumber, DateTimeOffset enqueuedTimeUtc)
        {
            var searchStartTime = DateTimeOffset.UtcNow;
            var originalSearchRequestId = identifiedRepeatSearchRequest.OriginalSearchId;
            var repeatSearchId = identifiedRepeatSearchRequest.RepeatSearchId;
            var searchAlgorithmServiceVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
            var hlaNomenclatureVersion = hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion();
            // Comes from the same cached record as the version (and the database that matching reads), so match prediction can
            // later tell which transient database these results came from.
            var dataRefreshRecordId = activeDataRefreshRecordAccessor.GetActiveRecord()?.Id;
            SearchResultDifferential diff = null;
            // Set when a results notification (success or failure) is published, the same as in SearchRunner; null if none is.
            DateTime? resultsSentTime = null;
            int? numberOfResults = null;
            MatchingAlgorithmFailureInfo matchingAlgorithmFailureInfo = null;

            repeatSearchLoggingContext.SearchRequestId = originalSearchRequestId;
            repeatSearchLoggingContext.HlaNomenclatureVersion = hlaNomenclatureVersion;

            matchingAlgorithmSearchTrackingContext.SearchIdentifier = new Guid(repeatSearchId);
            matchingAlgorithmSearchTrackingContext.OriginalSearchIdentifier = new Guid(originalSearchRequestId);
            matchingAlgorithmSearchTrackingContext.AttemptNumber = (byte)attemptNumber;

            // Must run before the search tracking initiation event: a wait is not an attempt of the repeat search.
            if (await WaitForOriginalSearchResultSetIfNotStored(identifiedRepeatSearchRequest, enqueuedTimeUtc))
            {
                return;
            }

            try
            {
                // Must run before validation: search tracking creates the attempt row on this event, and the completed event sent from
                // `finally` needs that row, else it is retried until dead-lettered.
                // After a wait for the original results, the request was queued at its first message, not at this one.
                var initiationTimeUtc = identifiedRepeatSearchRequest.FirstRequestTimeUtc ?? enqueuedTimeUtc;
                await searchTrackingEventPublisher.ProcessInitiation(initiationTimeUtc.UtcDateTime, searchStartTime.UtcDateTime);

                await repeatSearchValidator.ValidateRepeatSearchAndThrow(identifiedRepeatSearchRequest.RepeatSearchRequest);

                // ReSharper disable once PossibleInvalidOperationException - validation has ensured this is not null.
                var searchCutoffDate = identifiedRepeatSearchRequest.RepeatSearchRequest.SearchCutoffDate.Value;

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var results = (await searchService.Search(identifiedRepeatSearchRequest.RepeatSearchRequest.SearchRequest, searchCutoffDate))
                    .ToList();
                numberOfResults = results.Count;

                diff = await CalculateAndStoreResultsDiff(originalSearchRequestId, results, searchCutoffDate);

                await RecordRepeatSearch(identifiedRepeatSearchRequest, diff);

                stopwatch.Stop();

                var searchResultSet = new RepeatMatchingAlgorithmResultSet
                {
                    SearchRequest = identifiedRepeatSearchRequest.RepeatSearchRequest.SearchRequest,
                    SearchRequestId = originalSearchRequestId,
                    RepeatSearchId = repeatSearchId,
                    Results = results,
                    TotalResults = results.Count,
                    MatchingAlgorithmHlaNomenclatureVersion = hlaNomenclatureVersion,
                    MatchingAlgorithmDataRefreshRecordId = dataRefreshRecordId,
                    BlobStorageContainerName = azureStorageSettings.MatchingResultsBlobContainer,
                    NoLongerMatchingDonors = diff.RemovedResults.ToList(),
                    BatchedResult = azureStorageSettings.ShouldBatchResults,
                    MatchingStartTime = searchStartTime
                };

                await searchTrackingEventPublisher.ProcessPersistingResultsStarted();
                await repeatResultsBlobStorageClient.UploadResults(searchResultSet, azureStorageSettings.SearchResultsBatchSize,
                    $"{originalSearchRequestId}/{repeatSearchId}");
                await searchTrackingEventPublisher.ProcessPersistingResultsEnded();

                var notification = new MatchingResultsNotification
                {
                    SearchRequest = identifiedRepeatSearchRequest.RepeatSearchRequest.SearchRequest,
                    RepeatSearchRequestId = identifiedRepeatSearchRequest.RepeatSearchId,
                    SearchRequestId = originalSearchRequestId,
                    MatchingAlgorithmServiceVersion = searchAlgorithmServiceVersion,
                    MatchingAlgorithmHlaNomenclatureVersion = hlaNomenclatureVersion,
                    WasSuccessful = true,
                    NumberOfResults = results.Count,
                    BlobStorageContainerName = azureStorageSettings.MatchingResultsBlobContainer,
                    ResultsFileName = searchResultSet.ResultsFileName,
                    ElapsedTime = stopwatch.Elapsed,
                    ResultsBatched = azureStorageSettings.ShouldBatchResults,
                    BatchFolderName = azureStorageSettings.ShouldBatchResults && results.Any() ? $"{originalSearchRequestId}/{repeatSearchId}" : null
                };
                await repeatSearchServiceBusClient.PublishToResultsNotificationTopic(notification);
                resultsSentTime = DateTime.UtcNow;
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
                    $"Failed to run search with repeat search id: {repeatSearchId} (search id: {originalSearchRequestId}). Exception: {e}",
                    LogLevel.Error);
                await repeatSearchMatchingFailureNotificationSender.SendFailureNotification(identifiedRepeatSearchRequest, attemptNumber,
                    searchRequestMaxRetryCount - attemptNumber);
                resultsSentTime = DateTime.UtcNow;

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
                await searchTrackingEventPublisher.ProcessCompleted((
                    HlaNomenclatureVersion: hlaNomenclatureVersion,
                    ResultsSentTimeUtc: resultsSentTime,
                    NumberOfResults: numberOfResults,
                    FailureInfo: matchingAlgorithmFailureInfo,
                    RepeatSearchResultsDetails: new MatchingAlgorithmRepeatSearchResultsDetails
                    {
                        AddedResultCount = diff?.NewResults.Count,
                        RemovedResultCount = diff?.RemovedResults.Count,
                        UpdatedResultCount = diff?.UpdatedResults.Count
                    },
                    NumberOfMatching: numberOfResults
                ));
            }

            async Task HandleValidationExceptionWithoutRethrow(Exception ex)
            {
                repeatSearchLogger.SendTrace(
                    $"Validation failed for repeat search id: {repeatSearchId} (search id: {originalSearchRequestId}). Exception: {ex}",
                    LogLevel.Error);

                await repeatSearchMatchingFailureNotificationSender.SendFailureNotification(
                    identifiedRepeatSearchRequest, attemptNumber, 0, ex.Message);
                resultsSentTime = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// The result set of the original search can be stored after the repeat search is requested, for example when a large search
        /// is stored before it. Until the maximum wait, a repeat search whose original result set is not stored yet is published again
        /// with a delay, and the current message is completed. After the maximum wait, the repeat search runs, and validation fails it.
        /// </summary>
        /// <returns>True if the request was published again to wait, so this run must stop.</returns>
        private async Task<bool> WaitForOriginalSearchResultSetIfNotStored(
            IdentifiedRepeatSearchRequest identifiedRepeatSearchRequest,
            DateTimeOffset enqueuedTimeUtc)
        {
            var originalSearchId = identifiedRepeatSearchRequest.RepeatSearchRequest?.OriginalSearchId;

            // An invalid request fails validation at once: there is nothing to wait for.
            if (string.IsNullOrWhiteSpace(originalSearchId))
            {
                return false;
            }

            var firstRequestTimeUtc = identifiedRepeatSearchRequest.FirstRequestTimeUtc ?? enqueuedTimeUtc;
            var timeWaited = TimeSpan.FromSeconds(Math.Floor((DateTimeOffset.UtcNow - firstRequestTimeUtc).TotalSeconds));

            if (timeWaited >= maxWaitForOriginalResults || await repeatSearchValidator.IsOriginalSearchResultSetStored(originalSearchId))
            {
                return false;
            }

            identifiedRepeatSearchRequest.FirstRequestTimeUtc = firstRequestTimeUtc;
            await repeatSearchServiceBusClient.PublishToRepeatSearchRequestsTopic(
                identifiedRepeatSearchRequest, DateTimeOffset.UtcNow + originalResultsWaitInterval);

            repeatSearchLogger.SendTrace(
                $"Original search results are not stored yet for repeat search id: {identifiedRepeatSearchRequest.RepeatSearchId} " +
                $"(search id: {originalSearchId}). Waited {timeWaited} so far; the repeat search will run again in {originalResultsWaitInterval}.",
                LogLevel.Info,
                new Dictionary<string, string>
                {
                    { "OriginalSearchId", originalSearchId },
                    { "RepeatSearchId", identifiedRepeatSearchRequest.RepeatSearchId },
                    { "TimeWaitedSeconds", timeWaited.TotalSeconds.ToString(CultureInfo.InvariantCulture) }
                });

            return true;
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