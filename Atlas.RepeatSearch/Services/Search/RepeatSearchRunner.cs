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
            // Set when the success notification of this attempt is published: after that, the consumer has the results.
            var resultsNotificationPublished = false;
            // Set when this delivery stops after the diff because another delivery recorded the repeat search.
            var stoppedForAnotherDelivery = false;
            int? numberOfResults = null;
            MatchingAlgorithmFailureInfo matchingAlgorithmFailureInfo = null;

            repeatSearchLoggingContext.SearchRequestId = originalSearchRequestId;
            repeatSearchLoggingContext.HlaNomenclatureVersion = hlaNomenclatureVersion;

            matchingAlgorithmSearchTrackingContext.SearchIdentifier = new Guid(repeatSearchId);
            matchingAlgorithmSearchTrackingContext.OriginalSearchIdentifier = new Guid(originalSearchRequestId);
            matchingAlgorithmSearchTrackingContext.AttemptNumber = (byte)attemptNumber;

            // Must run before the wait and the search tracking initiation event: an earlier delivery completed the repeat search.
            if (await StopIfRepeatSearchIsRecorded(identifiedRepeatSearchRequest,
                    "an earlier delivery sent its results and changed the canonical result set."))
            {
                return;
            }

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

                // The canonical result set is changed only after the results notification is published. So a delivery that fails before
                // that leaves the set as it was, and the retry calculates the same diff.
                diff = await CalculateResultsDiff(originalSearchRequestId, results, searchCutoffDate);

                // Another delivery of this message can run at the same time: when the host shuts down, the function stops waiting for this
                // run and the message is delivered again, but this run goes on. If that delivery committed before this diff read the set, this
                // diff used the changed set, so its lists are wrong. The record commits with the set change, so this check finds it.
                if (await StopIfRepeatSearchIsRecorded(identifiedRepeatSearchRequest,
                        "another delivery changed the canonical result set while this delivery ran, so the diff of this delivery can be wrong."))
                {
                    stoppedForAnotherDelivery = true;
                    return;
                }

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
                resultsNotificationPublished = true;

                await ApplyResultsDiffAndRecordRepeatSearch(identifiedRepeatSearchRequest, searchCutoffDate, diff);
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
                if (resultsNotificationPublished)
                {
                    // The consumer has the results, and the set was not changed, so a retry sends the same results again. A failure
                    // notification between the two would tell the consumer that the repeat search failed.
                    repeatSearchLogger.SendTrace(
                        $"Results were sent for repeat search id: {repeatSearchId} (search id: {originalSearchRequestId}), but the canonical " +
                        $"result set was not changed. A retry sends the same results again. Exception: {e}",
                        LogLevel.Error);
                }
                else
                {
                    repeatSearchLogger.SendTrace(
                        $"Failed to run search with repeat search id: {repeatSearchId} (search id: {originalSearchRequestId}). Exception: {e}",
                        LogLevel.Error);
                    await repeatSearchMatchingFailureNotificationSender.SendFailureNotification(identifiedRepeatSearchRequest, attemptNumber,
                        searchRequestMaxRetryCount - attemptNumber);
                    resultsSentTime = DateTime.UtcNow;
                }

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
                // Search tracking writes the completed event onto the row of the repeat search. A delivery that stopped for another delivery
                // sent no results, so its event could be processed after the event of the delivery that sent the results, and clear the results
                // sent time and the counts of the diff. So it sends no event, and its attempt row stays without a completion.
                if (!stoppedForAnotherDelivery)
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

        /// <summary>
        /// A repeat search has a history record only after its results notification was published and its diff was applied to the canonical
        /// result set. A delivery of its request after that must not send results: its diff would use the changed set, so new donors would be
        /// counted as updated and the removed donors would be missing from the results. Such a delivery comes, for example, when the search
        /// tracking event in <c>finally</c> or the completion of the message fails, when two deliveries of the message run at the same time,
        /// or when support reruns a repeat search whose match prediction failed (see README_Support).
        /// </summary>
        /// <param name="identifiedRepeatSearchRequest">The request of this delivery.</param>
        /// <param name="reason">Why the record can exist at this point, for the trace.</param>
        /// <returns>True if the repeat search is already recorded, so this delivery must stop.</returns>
        private async Task<bool> StopIfRepeatSearchIsRecorded(IdentifiedRepeatSearchRequest identifiedRepeatSearchRequest, string reason)
        {
            var repeatSearchId = identifiedRepeatSearchRequest.RepeatSearchId;
            if (!await repeatSearchHistoryRepository.IsRepeatSearchRecorded(repeatSearchId))
            {
                return false;
            }

            var originalSearchId = identifiedRepeatSearchRequest.OriginalSearchId;
            repeatSearchLogger.SendTrace(
                $"Repeat search id: {repeatSearchId} (search id: {originalSearchId}) is already recorded: {reason} " +
                "This delivery sends no results.",
                LogLevel.Warn,
                new Dictionary<string, string>
                {
                    { "OriginalSearchId", originalSearchId },
                    { "RepeatSearchId", repeatSearchId }
                });

            return true;
        }

        private async Task<SearchResultDifferential> CalculateResultsDiff(
            string searchRequestId,
            List<MatchingAlgorithmResult> results,
            DateTimeOffset searchCutoffDate)
        {
            using (repeatSearchLogger.RunTimed("Calculate result diff"))
            {
                var diff = await repeatSearchDifferentialCalculator.CalculateDifferential(searchRequestId, results, searchCutoffDate);

                repeatSearchLogger.SendTrace(
                    $"Donor Result Diff Calculated. {diff.NewResults.Count} new results. {diff.UpdatedResults.Count} updated results. {diff.RemovedResults.Count} removed results."
                );

                return diff;
            }
        }

        private async Task ApplyResultsDiffAndRecordRepeatSearch(
            IdentifiedRepeatSearchRequest identifiedRepeatSearchRequest,
            DateTimeOffset searchCutoffDate,
            SearchResultDifferential searchResultDifferential)
        {
            using (repeatSearchLogger.RunTimed("Apply result diff to canonical result set and record repeat search"))
            {
                var historyRecord = new RepeatSearchHistoryRecord
                {
                    DateCreated = DateTimeOffset.UtcNow,
                    SearchCutoffDate = searchCutoffDate,
                    OriginalSearchRequestId = identifiedRepeatSearchRequest.OriginalSearchId,
                    RepeatSearchRequestId = identifiedRepeatSearchRequest.RepeatSearchId,
                    AddedResultCount = searchResultDifferential.NewResults.Count,
                    UpdatedResultCount = searchResultDifferential.UpdatedResults.Count,
                    RemovedResultCount = searchResultDifferential.RemovedResults.Count,
                };

                if (!await originalSearchResultSetTracker.ApplySearchResultDiff(historyRecord, searchResultDifferential))
                {
                    repeatSearchLogger.SendTrace(
                        $"Repeat search id: {historyRecord.RepeatSearchRequestId} (search id: {historyRecord.OriginalSearchRequestId}) was " +
                        "recorded by another delivery. The canonical result set is not changed again.",
                        LogLevel.Warn);
                }
            }
        }
    }
}