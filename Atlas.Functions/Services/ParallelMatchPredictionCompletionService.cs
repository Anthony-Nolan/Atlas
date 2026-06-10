using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.LogFile;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.AzureStorage.Blob;
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
    Task CompleteRun(int runId);
}

public class ParallelMatchPredictionCompletionService : IParallelMatchPredictionCompletionService
{
    private readonly IParallelMatchPredictionRepository repository;
    private readonly IMatchingResultsDownloader matchingResultsDownloader;
    private readonly IResultsCombiner resultsCombiner;
    private readonly ISearchResultsBlobStorageClient searchResultsBlobUploader;
    private readonly ISearchCompletionMessageSender searchCompletionMessageSender;
    private readonly IMatchPredictionSearchTrackingDispatcher matchPredictionSearchTrackingDispatcher;
    private readonly IAtlasLogger logger;
    private readonly AzureStorageSettings azureStorageSettings;
    private readonly int parallelMatchPredictionBatchSize;

    public ParallelMatchPredictionCompletionService(
        IParallelMatchPredictionRepository repository,
        IMatchingResultsDownloader matchingResultsDownloader,
        IResultsCombiner resultsCombiner,
        ISearchResultsBlobStorageClient searchResultsBlobUploader,
        ISearchCompletionMessageSender searchCompletionMessageSender,
        IMatchPredictionSearchTrackingDispatcher matchPredictionSearchTrackingDispatcher,
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
        this.logger = logger;
        this.azureStorageSettings = azureStorageSettings.Value;
        parallelMatchPredictionBatchSize = orchestrationSettings.Value.ParallelMatchPredictionBatchSize;
    }

    public async Task CompleteRun(int runId)
    {
        var runResults = await repository.GetRunWithResults(runId);
        if (runResults is null)
        {
            logger.SendTrace($"Parallel match prediction run with id {runId} not found; skipping finalisation.", LogLevel.Warn);
            return;
        }

        var run = runResults.Run;
        var resultsLocations = runResults.MergedResultLocations;

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
                await CompleteFailedRun(runId, run, runResults, trackingSearchIdentifier, originalSearchIdentifier);
                return;
            }

            await CompleteSuccessfulRun(runId, run, resultsLocations, trackingSearchIdentifier, originalSearchIdentifier);
        }
        catch
        {
            await repository.MarkRunFailedDuringCompletion(runId, DateTime.UtcNow);
            throw;
        }
    }

    private async Task CompleteFailedRun(
        int runId,
        Atlas.MatchPrediction.Data.Models.ParallelMatchPredictionRun run,
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

        // Emit failure notification.
        await searchCompletionMessageSender.PublishFailureMessage(new SendFailureNotificationParameters
            {
                SearchRequestId = run.SearchIdentifier.ToString(),
                RepeatSearchRequestId = run.RepeatSearchIdentifier?.ToString(),
                StageReached = "MatchPredictionBatchProcessing",
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
        Atlas.MatchPrediction.Data.Models.ParallelMatchPredictionRun run,
        IReadOnlyDictionary<int, string> resultsLocations,
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

        resultSet.Results = await logger.RunTimedAsync("Combining search results", async () =>
            run.ResultsBatched
                ? await CombineBatchedSearchResults(
                    resultSet.SearchRequestId,
                    run.IsRepeatSearch,
                    resultsLocations,
                    run.BatchFolderName,
                    resultSet.BlobStorageContainerName,
                    azureStorageSettings.ShouldBatchResults
                )
                : await resultsCombiner.CombineResults(resultSet.SearchRequestId, matchingResultsSummary.Results, resultsLocations)
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
        IReadOnlyDictionary<int, string> matchPredictionResultLocations,
        string batchFolder,
        string blobStorageContainerName,
        bool resultsShouldBeBatched)
    {
        var allSearchResults = new List<SearchResult>();
        var batchNumber = 0;

        await foreach (var matchingResults in matchingResultsDownloader.DownloadResults(isRepeatSearch, batchFolder))
        {
            var matchingAlgorithmResults = matchingResults.ToList();
            var donorIds = matchingAlgorithmResults.Select(r => r.AtlasDonorId).ToList();
            var matchPredictionResultLocationsForCurrentDonors =
                matchPredictionResultLocations.Where(l => donorIds.Contains(l.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
            var currentSearchResults =
                await resultsCombiner.CombineResults(searchRequestId, matchingAlgorithmResults, matchPredictionResultLocationsForCurrentDonors);

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