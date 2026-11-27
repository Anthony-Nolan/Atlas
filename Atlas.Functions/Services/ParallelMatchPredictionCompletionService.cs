using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.LogFile;
using Atlas.Client.Models.SupportMessages;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Common.Notifications;
using Atlas.Functions.Models;
using Atlas.Functions.Services.BlobStorageClients;
using Atlas.Functions.Settings;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.SearchTracking.Common.Dispatchers;
using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Common.Models;
using Microsoft.Extensions.Options;

namespace Atlas.Functions.Services;

public interface IParallelMatchPredictionCompletionService
{
    /// <summary>
    /// Runs the final persistence pipeline for a single completed run: combines per-batch MPA results with matching
    /// results, uploads the combined result set to blob storage, sends the completion notification, uploads the search
    /// log, marks the run finalised in the <see cref="IParallelMatchPredictionRepository"/>, and emits the
    /// match-prediction completion tracking event.
    /// </summary>
    /// <remarks>
    /// All batches succeeded → <see cref="ParallelMatchPredictionRunStatus.Finalised"/>
    /// (<see cref="ParallelMatchPredictionRun.IsSuccessful"/> = <c>true</c>). Any batch failed →
    /// <see cref="ParallelMatchPredictionRunStatus.FailedDuringBatchProcessing"/>
    /// (<see cref="ParallelMatchPredictionRun.IsSuccessful"/> = <c>false</c>), with performance metrics, failure
    /// notification, search log and tracking event still emitted. Any pipeline step throwing →
    /// <see cref="ParallelMatchPredictionRunStatus.FailedDuringCompletion"/> (if that mark also fails, the run keeps
    /// its status and lease), then a failed search-results-ready message, a failed match-prediction completion tracking
    /// event and a High-priority alert are sent (each message only if the run did not already send one of its kind),
    /// and the original exception is rethrown. See
    /// <see cref="ParallelMatchPredictionRunStatus"/> for how the finaliser then handles each terminal state.
    /// </remarks>
    Task FinaliseRun(int runId);

    /// <summary>
    /// Abandons a single run whose batches did not all return within the configured timeout: transitions it to
    /// <see cref="ParallelMatchPredictionRunStatus.Abandoned"/> and publishes a single downstream failure notification
    /// (<c>WasSuccessful=false</c>). The repository status compare-and-swap guards against double-processing, so an
    /// overlapping tick that finds the run no longer <see cref="ParallelMatchPredictionRunStatus.Running"/> is a no-op.
    /// </summary>
    Task AbandonRun(int runId);
}

public class ParallelMatchPredictionCompletionService : IParallelMatchPredictionCompletionService
{
    private readonly IParallelMatchPredictionRepository repository;
    private readonly IMatchingResultsDownloader matchingResultsDownloader;
    private readonly IResultsCombiner resultsCombiner;
    private readonly ISearchResultsBlobStorageClient searchResultsBlobUploader;
    private readonly ISearchCompletionMessageSender searchCompletionMessageSender;
    private readonly IMatchPredictionSearchTrackingDispatcher matchPredictionSearchTrackingDispatcher;
    private readonly INotificationSender notificationSender;
    private readonly IAtlasLogger logger;
    private readonly AzureStorageSettings azureStorageSettings;
    private readonly int parallelMatchPredictionBatchSize;

    private const string ParallelMatchPredictionNotificationSource = "Atlas.Functions.ParallelMatchPrediction";
    private const string MatchPredictionBatchProcessingStage = "MatchPredictionBatchProcessing";

    private const string CompletionFailureStage = "MatchPredictionCompletion";

    private const string AbandonmentReason = "one or more batches did not return a result within the configured timeout.";

    public ParallelMatchPredictionCompletionService(
        IParallelMatchPredictionRepository repository,
        IMatchingResultsDownloader matchingResultsDownloader,
        IResultsCombiner resultsCombiner,
        ISearchResultsBlobStorageClient searchResultsBlobUploader,
        ISearchCompletionMessageSender searchCompletionMessageSender,
        IMatchPredictionSearchTrackingDispatcher matchPredictionSearchTrackingDispatcher,
        INotificationSender notificationSender,
        ISearchLogger<SearchLoggingContext> logger,
        IOptions<AzureStorageSettings> azureStorageSettings,
        IOptions<OrchestrationSettings> orchestrationSettings)
    {
        this.repository = repository;
        this.matchingResultsDownloader = matchingResultsDownloader;
        this.resultsCombiner = resultsCombiner;
        this.searchResultsBlobUploader = searchResultsBlobUploader;
        this.searchCompletionMessageSender = searchCompletionMessageSender;
        this.matchPredictionSearchTrackingDispatcher = matchPredictionSearchTrackingDispatcher;
        this.notificationSender = notificationSender;
        this.logger = logger;
        this.azureStorageSettings = azureStorageSettings.Value;
        parallelMatchPredictionBatchSize = orchestrationSettings.Value.ParallelMatchPredictionBatchSize;
    }

    public async Task FinaliseRun(int runId)
    {
        var runResults = await repository.GetRunWithResults(runId);
        if (runResults is null)
        {
            logger.SendTrace($"Parallel match prediction run with id {runId} not found; skipping finalisation.", LogLevel.Warn);
            return;
        }

        var run = runResults.Run;
        var trackingSearchIdentifier = run.IsRepeatSearch
            ? run.RepeatSearchIdentifier!.Value
            : run.SearchIdentifier;
        var originalSearchIdentifier = run.IsRepeatSearch
            ? run.SearchIdentifier
            : (Guid?)null;
        var progress = new FinalisationProgress();

        try
        {
            if (runResults.FailedBatches.Count > 0)
            {
                // On replay (see ParallelMatchPredictionRunStatus) a previously-Abandoned run reaches here once its
                // late batch results arrive. If any late result is a failure we deliberately re-run the failure path:
                // the abandonment failure was only provisional, so ProcessCompleted supersedes the provisional
                // Abandoned tracking with the definitive, more-informative failure (real BatchWorkerFailure cause and
                // exception detail). The run lands in FailedDuringBatchProcessing like any normal failed run.
                // Consumers may therefore see the provisional abandonment failure followed by this confirmed failure —
                // accepted and documented, mirroring the success replay.
                await CompleteFailedRun(runId, run, runResults, trackingSearchIdentifier, originalSearchIdentifier, progress);
                return;
            }

            await CompleteSuccessfulRun(
                runId, run, runResults.BatchResultLocations, trackingSearchIdentifier, originalSearchIdentifier, progress
            );
        }
        catch (Exception ex)
        {
            // The mark is tried on its own: if it fails too (for example, the original failure was the database), the
            // run keeps its finalisation lease and its old status. The sweep, abandonment and cleanup all skip a leased
            // run, so it is never picked up again either way, and the report below is still the last chance to tell
            // the caller. The original exception is rethrown, not the exception of the mark.
            var markedFailedDuringCompletion = await TryMarkRunFailedDuringCompletion(runId, run);
            await ReportCompletionFailure(
                runId, run, trackingSearchIdentifier, originalSearchIdentifier, progress, ex, markedFailedDuringCompletion
            );
            throw;
        }
    }

    private async Task<bool> TryMarkRunFailedDuringCompletion(int runId, ParallelMatchPredictionRun run)
    {
        try
        {
            await repository.MarkRunFailedDuringCompletion(runId, DateTime.UtcNow);
            return true;
        }
        catch (Exception ex)
        {
            logger.SendTrace(
                $"Failed to mark parallel match prediction run {runId} (search {run.SearchIdentifier}) as "
              + $"FailedDuringCompletion: {ex.Message}",
                LogLevel.Error
            );
            return false;
        }
    }

    /// <summary>
    /// A run that failed during completion is never finalised again, so this is the last chance to tell the caller,
    /// Search Tracking and support that the search ended. The caller gets a short failure detail without the exception
    /// text, which can hold storage URIs or server names; the full text goes to Search Tracking and the alert. Each
    /// Service Bus send is tried on its own, so one failed send does not stop the others; the caller still rethrows
    /// the original exception.
    /// </summary>
    private async Task ReportCompletionFailure(
        int runId,
        ParallelMatchPredictionRun run,
        Guid trackingSearchIdentifier,
        Guid? originalSearchIdentifier,
        FinalisationProgress progress,
        Exception exception,
        bool markedFailedDuringCompletion)
    {
        var callerFailureDetail = $"Parallel match prediction run {runId} failed during completion.";
        var failureMessage =
            $"Parallel match prediction run {runId} (search {run.SearchIdentifier}) failed during completion: {exception.Message}";

        logger.SendTrace(failureMessage, LogLevel.Error);

        // No failed completion event if one was already sent (success, or a batch failure with its real cause): Search
        // Tracking overwrites the failure info on each completion, so a second one could replace it. Nor if the caller
        // already has its result message: Search Tracking must not record a failure for a search the caller got.
        if (!progress.TrackingCompletionSent && !progress.ResultMessagePublished)
        {
            await TrySend("match-prediction completion tracking event", run, () =>
                matchPredictionSearchTrackingDispatcher.ProcessCompleted(
                    (
                        trackingSearchIdentifier,
                        originalSearchIdentifier,
                        IsSuccessful: false,
                        FailureInfo: new MatchPredictionFailureInfo
                        {
                            Type = MatchPredictionFailureType.UnexpectedError,
                            Message = failureMessage,
                            ExceptionStacktrace = exception.ToString(),
                        },
                        DonorsPerBatch: parallelMatchPredictionBatchSize,
                        TotalNumberOfBatches: run.TotalBatchCount
                    )
                )
            );
        }

        // If the result message (success or failure) was already published, the caller already has a result for the
        // search: do not send a second message that contradicts it.
        var failureMessageSent = false;
        if (!progress.ResultMessagePublished)
        {
            failureMessageSent = await TrySend("failed search-results-ready message", run, () =>
                searchCompletionMessageSender.PublishFailureMessage(new SendFailureNotificationParameters
                    {
                        SearchRequestId = run.SearchIdentifier.ToString(),
                        RepeatSearchRequestId = run.RepeatSearchIdentifier?.ToString(),
                        StageReached = CompletionFailureStage,
                        FailureDetail = callerFailureDetail,
                    }
                )
            );
        }

        var runOutcome = markedFailedDuringCompletion
            ? "The run is now FailedDuringCompletion and will not be finalised again."
            : "The run could not be marked FailedDuringCompletion either: it keeps its old status and its finalisation "
            + "lease, so it will not be finalised again.";
        var callerOutcome = progress.ResultMessagePublished
            ? "The search result message had already been sent, so the caller has a result."
            : failureMessageSent
                ? "A failed search-results-ready message was sent to the caller."
                : "The failed search-results-ready message could not be sent either: the caller has no result for this search.";

        // NotificationSender does not leak exceptions, so the alert is not wrapped.
        await notificationSender.SendAlert(
            $"Parallel match prediction failed during completion for search {run.SearchIdentifier}",
            $"{failureMessage} (parallel run id: {runId}). {runOutcome} {callerOutcome} "
          + "Full exception detail is available in Application Insights.",
            Priority.High,
            ParallelMatchPredictionNotificationSource
        );
    }

    private async Task<bool> TrySend(string description, ParallelMatchPredictionRun run, Func<Task> send)
    {
        try
        {
            await send();
            return true;
        }
        catch (Exception ex)
        {
            logger.SendTrace(
                $"Failed to send the {description} for search {run.SearchIdentifier} after a completion failure: {ex.Message}",
                LogLevel.Error
            );
            return false;
        }
    }

    public async Task AbandonRun(int runId)
    {
        // The repository performs a status compare-and-swap (Running -> Abandoned). Only the winning caller gets a
        // header back, so we mark first and notify only on a real transition — this prevents a duplicate failure
        // notification when two timer ticks overlap.
        var header = await repository.TryMarkRunAsAbandoned(runId, DateTime.UtcNow);
        if (header is null)
        {
            logger.SendTrace($"Parallel match prediction run {runId} was no longer in the Running state; skipping abandonment.");
            return;
        }

        // Match the repeat-search identifier convention used by FinaliseRun so tracking events line up.
        var trackingSearchIdentifier = header.IsRepeatSearch
            ? header.RepeatSearchIdentifier!.Value
            : header.SearchIdentifier;
        var originalSearchIdentifier = header.IsRepeatSearch
            ? header.SearchIdentifier
            : (Guid?)null;

        var failureMessage =
            $"Parallel match prediction run {runId} (search {header.SearchIdentifier}) was abandoned: {AbandonmentReason}";

        // Emit the completion tracking event so search tracking records the run as an abandonment (rather than never
        // recording a terminal state). Uses the dedicated Abandoned failure type to distinguish it from a batch failure.
        await matchPredictionSearchTrackingDispatcher.ProcessCompleted(
            (
                trackingSearchIdentifier,
                originalSearchIdentifier,
                IsSuccessful: false,
                FailureInfo: new MatchPredictionFailureInfo
                {
                    Type = MatchPredictionFailureType.Abandoned,
                    Message = failureMessage,
                },
                DonorsPerBatch: parallelMatchPredictionBatchSize,
                TotalNumberOfBatches: header.TotalBatchCount
            )
        );

        // Publish the single downstream failure notification, carrying the abandonment detail in the shared payload.
        await searchCompletionMessageSender.PublishFailureMessage(new SendFailureNotificationParameters
            {
                SearchRequestId = header.SearchIdentifier.ToString(),
                RepeatSearchRequestId = header.RepeatSearchIdentifier?.ToString(),
                StageReached = MatchPredictionBatchProcessingStage,
                FailureDetail = failureMessage,
            }
        );

        // Raise an operational alert so an abandoned search does not fail silently for support.
        await notificationSender.SendAlert(
            $"Parallel match prediction run abandoned (search {header.SearchIdentifier})",
            failureMessage,
            Priority.Medium,
            ParallelMatchPredictionNotificationSource
        );

        logger.SendTrace(
            $"Abandoned parallel match prediction run {runId} (search {header.SearchIdentifier}): {AbandonmentReason} "
          + "Failure notification, tracking event and alert sent.",
            LogLevel.Warn
        );
    }

    private async Task CompleteFailedRun(
        int runId,
        ParallelMatchPredictionRun run,
        ParallelMatchPredictionRunResults runResults,
        Guid trackingSearchIdentifier,
        Guid? originalSearchIdentifier,
        FinalisationProgress progress)
    {
        var failedBatchCount = runResults.FailedBatches.Count;
        var failureMessage = $"{failedBatchCount} out of {run.TotalBatchCount} match prediction batch(es) failed during processing.";

        logger.SendTrace(failureMessage, LogLevel.Error);

        // Still upload the search/performance log even on failure.
        try
        {
            var searchLog = new SearchLog
            {
                SearchRequestId = run.SearchIdentifier.ToString(),
                WasSuccessful = false,
                SearchRequest = null,
                RequestPerformanceMetrics = new RequestPerformanceMetrics
                {
                    InitiationTime = run.SearchInitiatedTimeUtc,
                    StartTime = run.SearchInitiatedTimeUtc,
                    CompletionTime = DateTime.UtcNow,
                }
            };
            await searchResultsBlobUploader.UploadResults(
                searchLog,
                azureStorageSettings.SearchResultsBlobContainer,
                $"{run.SearchIdentifier}-log.json"
            );
        }
        catch
        {
            logger.SendTrace(
                $"Failed to write performance log file for search with id {run.SearchIdentifier}.",
                LogLevel.Error
            );
        }

        // Emit failure tracking event.
        var exceptionDetails = string.Join(
            "\n---\n",
            runResults.FailedBatches
                .Select(b => b.FailureException)
                .Where(e => !string.IsNullOrEmpty(e))
        );

        await matchPredictionSearchTrackingDispatcher.ProcessCompleted(
            (
                trackingSearchIdentifier,
                originalSearchIdentifier,
                IsSuccessful: false,
                FailureInfo: new MatchPredictionFailureInfo
                {
                    Type = MatchPredictionFailureType.BatchWorkerFailure,
                    Message = failureMessage,
                    ExceptionStacktrace = exceptionDetails,
                },
                DonorsPerBatch: parallelMatchPredictionBatchSize,
                TotalNumberOfBatches: run.TotalBatchCount
            )
        );
        progress.TrackingCompletionSent = true;

        var batchFailureReasons = runResults.FailedBatches
            .Select(b => b.FailureMessage)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct()
            .ToList();
        var failureDetail = batchFailureReasons.Count > 0
            ? $"{failureMessage} Reason(s): {string.Join("; ", batchFailureReasons)}"
            : failureMessage;

        // Emit failure notification, carrying the failure detail in the shared payload.
        await searchCompletionMessageSender.PublishFailureMessage(new SendFailureNotificationParameters
            {
                SearchRequestId = run.SearchIdentifier.ToString(),
                RepeatSearchRequestId = run.RepeatSearchIdentifier?.ToString(),
                StageReached = MatchPredictionBatchProcessingStage,
                FailureDetail = failureDetail,
            }
        );
        progress.ResultMessagePublished = true;

        logger.SendTrace(
            $"Failure notification sent for search {run.SearchIdentifier}: {failureDetail}",
            LogLevel.Error
        );

        // Raise an operational alert so a batch-worker failure does not fail silently for support.
        await notificationSender.SendAlert(
            $"Parallel match prediction failed for search {run.SearchIdentifier}",
            $"{failureMessage} (parallel run id: {runId}). Full exception detail is available in Application Insights "
          + "and in Search Tracking (match-prediction completion FailureInfo.ExceptionStacktrace).",
            Priority.High,
            ParallelMatchPredictionNotificationSource
        );

        await repository.MarkRunFailed(runId, DateTime.UtcNow);
    }

    private async Task CompleteSuccessfulRun(
        int runId,
        ParallelMatchPredictionRun run,
        IReadOnlyList<string> batchResultLocations,
        Guid trackingSearchIdentifier,
        Guid? originalSearchIdentifier,
        FinalisationProgress progress)
    {
        await matchPredictionSearchTrackingDispatcher.ProcessPersistingResultsStarted(
            trackingSearchIdentifier, originalSearchIdentifier
        );

        var matchingResultsSummary = await logger.RunTimedAsync(
            "Download matching results summary",
            async () => await matchingResultsDownloader.DownloadSummary(
                run.ResultsFileName,
                run.IsRepeatSearch
            )
        );

        // The parallel path does not provide match prediction time, so set to zero
        var resultSet = resultsCombiner.BuildResultsSummary(
            matchingResultsSummary, TimeSpan.Zero, run.MatchingAlgorithmElapsedTime
        );

        resultSet.BlobStorageContainerName = resultSet.IsRepeatSearchSet
            ? azureStorageSettings.RepeatSearchResultsBlobContainer
            : azureStorageSettings.SearchResultsBlobContainer;
        resultSet.BatchedResult = run.ResultsBatched && azureStorageSettings.ShouldBatchResults;

        // Download the per-batch result blobs and merge into one donor → result map.
        var matchPredictionResults = await logger.RunTimedAsync(
            "Download match prediction batch results",
            async () => await resultsCombiner.DownloadBatchedMatchPredictionResults(batchResultLocations)
        );

        resultSet.Results = await logger.RunTimedAsync("Combining search results", async () =>
            run.ResultsBatched
                ? await CombineBatchedSearchResults(
                    resultSet.SearchRequestId,
                    run.IsRepeatSearch,
                    matchPredictionResults,
                    run.BatchFolderName,
                    resultSet.BlobStorageContainerName,
                    azureStorageSettings.ShouldBatchResults
                )
                : resultsCombiner.CombineResults(resultSet.SearchRequestId, matchingResultsSummary.Results, matchPredictionResults)
        );

        await searchResultsBlobUploader.UploadResults(
            resultSet, resultSet.BlobStorageContainerName, resultSet.ResultsFileName
        );
        await matchPredictionSearchTrackingDispatcher.ProcessPersistingResultsEnded(
            trackingSearchIdentifier, originalSearchIdentifier
        );
        await searchCompletionMessageSender.PublishResultsMessage(
            resultSet, run.SearchInitiatedTimeUtc, run.BatchFolderName
        );
        progress.ResultMessagePublished = true;
        await matchPredictionSearchTrackingDispatcher.ProcessResultsSent(
            trackingSearchIdentifier, originalSearchIdentifier
        );

        // Upload search log (SearchRequest is not available on the parallel path – logged as null)
        try
        {
            var searchLog = new SearchLog
            {
                SearchRequestId = run.SearchIdentifier.ToString(),
                WasSuccessful = true,
                SearchRequest = null,
                RequestPerformanceMetrics = new RequestPerformanceMetrics
                {
                    InitiationTime = run.SearchInitiatedTimeUtc,
                    StartTime = run.SearchInitiatedTimeUtc,
                    CompletionTime = DateTime.UtcNow,
                }
            };
            await searchResultsBlobUploader.UploadResults(
                searchLog,
                azureStorageSettings.SearchResultsBlobContainer,
                $"{run.SearchIdentifier}-log.json"
            );
        }
        catch
        {
            logger.SendTrace(
                $"Failed to write performance log file for search with id {run.SearchIdentifier}.",
                LogLevel.Error
            );
        }

        // Send match prediction process completed event
        await matchPredictionSearchTrackingDispatcher.ProcessCompleted(
            (
                trackingSearchIdentifier,
                originalSearchIdentifier,
                IsSuccessful: true,
                FailureInfo: null,
                DonorsPerBatch: parallelMatchPredictionBatchSize,
                TotalNumberOfBatches: run.TotalBatchCount
            )
        );
        progress.TrackingCompletionSent = true;

        await notificationSender.SendNotification(
            $"Parallel match prediction completed for search {run.SearchIdentifier}",
            $"Parallel match prediction run {runId} (search {run.SearchIdentifier}) finalised successfully "
          + $"across {run.TotalBatchCount} batch(es).",
            ParallelMatchPredictionNotificationSource
        );

        await repository.MarkRunFinalised(runId, DateTime.UtcNow);
    }

    private async Task<IEnumerable<SearchResult>> CombineBatchedSearchResults(
        string searchRequestId,
        bool isRepeatSearch,
        IReadOnlyDictionary<int, MatchProbabilityResponse> matchPredictionResults,
        string batchFolder,
        string blobStorageContainerName,
        bool resultsShouldBeBatched)
    {
        var allSearchResults = new List<SearchResult>();
        var batchNumber = 0;

        await foreach (var matchingResults in matchingResultsDownloader.DownloadResults(isRepeatSearch, batchFolder))
        {
            var matchingAlgorithmResults = matchingResults.ToList();
            var matchPredictionResultsForCurrentDonors = new Dictionary<int, MatchProbabilityResponse>();
            foreach (var result in matchingAlgorithmResults)
            {
                if (matchPredictionResults.TryGetValue(result.AtlasDonorId, out var matchPredictionResult))
                {
                    matchPredictionResultsForCurrentDonors[result.AtlasDonorId] = matchPredictionResult;
                }
            }
            var currentSearchResults =
                resultsCombiner.CombineResults(searchRequestId, matchingAlgorithmResults, matchPredictionResultsForCurrentDonors);

            if (resultsShouldBeBatched)
            {
                await searchResultsBlobUploader.UploadResults(
                    currentSearchResults, blobStorageContainerName, $"{batchFolder}/{++batchNumber}.json"
                );
            }
            else
            {
                allSearchResults.AddRange(currentSearchResults);
            }
        }

        return allSearchResults;
    }

    /// <summary>Records how far one <see cref="FinaliseRun"/> call got, for the completion-failure path.</summary>
    private sealed class FinalisationProgress
    {
        /// <summary>True once the search's result message (success or failure) has been published to the caller.</summary>
        public bool ResultMessagePublished { get; set; }

        /// <summary>
        /// True once the match-prediction completion tracking event (success or failure) has been sent. Search Tracking
        /// overwrites the failure info on each completion, so a second one could replace the real cause.
        /// </summary>
        public bool TrackingCompletionSent { get; set; }
    }
}
