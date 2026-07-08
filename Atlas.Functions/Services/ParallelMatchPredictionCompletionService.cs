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
    /// Performs the final persistence pipeline for a single completed parallel match-prediction run:
    /// combines per-batch MPA results with matching results, uploads the combined result set to blob storage,
    /// sends the completion notification, uploads the search log, marks the run as finalised in the
    /// <see cref="IParallelMatchPredictionRepository"/>, and emits the match-prediction completion tracking event.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If all batches succeeded the run is marked <see cref="Atlas.MatchPrediction.Data.Models.ParallelMatchPredictionRunStatus.Finalised"/>
    /// and <see cref="Atlas.MatchPrediction.Data.Models.ParallelMatchPredictionRun.IsSuccessful"/> is set to <c>true</c>.
    /// </para>
    /// <para>
    /// If any batch failed the run is marked <see cref="ParallelMatchPredictionRunStatus.FailedDuringBatchProcessing"/>
    /// and <see cref="Atlas.MatchPrediction.Data.Models.ParallelMatchPredictionRun.IsSuccessful"/> is set to <c>false</c>.
    /// Performance metrics, the failure notification, search logs and the tracking event are still emitted.
    /// </para>
    /// <para>
    /// If any step of the pipeline throws unexpectedly, the run is marked
    /// <see cref="Atlas.MatchPrediction.Data.Models.ParallelMatchPredictionRunStatus.FailedDuringCompletion"/>
    /// and the exception is rethrown. The finalisation timer will not re-pick a failed run.
    /// </para>
    /// </remarks>
    Task FinaliseRun(int runId);

    /// <summary>
    /// Abandons a single parallel match-prediction run whose batches never all returned within the configured
    /// timeout: transitions the run to <see cref="ParallelMatchPredictionRunStatus.Abandoned"/> (marking its
    /// still-pending batches <see cref="Atlas.MatchPrediction.Data.Models.ParallelMatchPredictionBatchStatus.Abandoned"/>
    /// and setting <see cref="Atlas.MatchPrediction.Data.Models.ParallelMatchPredictionRun.IsSuccessful"/> to
    /// <c>false</c>) and publishes a single downstream failure notification (<c>WasSuccessful=false</c>).
    /// The status compare-and-swap in the repository guards against double-processing; if the run is no longer
    /// <c>Running</c> (already abandoned/finalised by another tick) this is a no-op.
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

    private const string AbandonmentNotificationSource = "Atlas.Functions.ParallelMatchPrediction";
    private const string MatchPredictionBatchProcessingStage = "MatchPredictionBatchProcessing";

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

        try
        {
            var trackingSearchIdentifier = run.IsRepeatSearch
                ? run.RepeatSearchIdentifier!.Value
                : run.SearchIdentifier;
            var originalSearchIdentifier = run.IsRepeatSearch
                ? run.SearchIdentifier
                : (Guid?)null;

            if (runResults.FailedBatches.Count > 0)
            {
                // A run that was previously Abandoned can be re-picked here once all of its late batch results have
                // finally arrived. If any of those late results is a failure, we deliberately run the failure path
                // again rather than suppressing it: the abandonment failure is only provisional (the late-all-success
                // path below likewise supersedes it with a result message), so once every batch has reported we send
                // the definitive outcome. The replayed failure is strictly more informative than the abandonment one —
                // it carries the real BatchWorkerFailure cause and exception detail, and ProcessCompleted supersedes
                // the provisional Abandoned tracking with the true failure. The run lands in FailedDuringBatchProcessing,
                // identical to any normal failed run. Consumers may therefore receive the provisional abandonment
                // failure followed by this confirmed failure — accepted and documented, mirroring the success replay.
                await CompleteFailedRun(runId, run, runResults, trackingSearchIdentifier, originalSearchIdentifier);
                return;
            }

            await CompleteSuccessfulRun(runId, run, runResults.BatchResultLocations, trackingSearchIdentifier, originalSearchIdentifier);
        }
        catch
        {
            await repository.MarkRunFailedDuringCompletion(runId, DateTime.UtcNow);
            throw;
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
            $"Parallel match prediction run {runId} (search {header.SearchIdentifier}) was abandoned: "
          + "one or more batches did not return a result within the configured timeout.";

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
            AbandonmentNotificationSource
        );

        logger.SendTrace(
            $"Abandoned parallel match prediction run {runId} (search {header.SearchIdentifier}): "
          + "one or more batches did not return within the configured timeout. "
          + "Failure notification, tracking event and alert sent.",
            LogLevel.Warn
        );
    }

    private async Task CompleteFailedRun(
        int runId,
        ParallelMatchPredictionRun run,
        ParallelMatchPredictionRunResults runResults,
        Guid trackingSearchIdentifier,
        Guid? originalSearchIdentifier)
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

        // Emit failure notification, carrying the failure detail in the shared payload.
        await searchCompletionMessageSender.PublishFailureMessage(new SendFailureNotificationParameters
            {
                SearchRequestId = run.SearchIdentifier.ToString(),
                RepeatSearchRequestId = run.RepeatSearchIdentifier?.ToString(),
                StageReached = MatchPredictionBatchProcessingStage,
                FailureDetail = failureMessage,
            }
        );

        logger.SendTrace(
            $"Failure notification sent for search {run.SearchIdentifier}: {failureMessage}",
            LogLevel.Error
        );

        await repository.MarkRunFailed(runId, DateTime.UtcNow);
    }

    private async Task CompleteSuccessfulRun(
        int runId,
        ParallelMatchPredictionRun run,
        IReadOnlyList<string> batchResultLocations,
        Guid trackingSearchIdentifier,
        Guid? originalSearchIdentifier)
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
            var donorIds = matchingAlgorithmResults.Select(r => r.AtlasDonorId).ToHashSet();
            var matchPredictionResultsForCurrentDonors =
                matchPredictionResults.Where(r => donorIds.Contains(r.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
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
}