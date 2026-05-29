using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.LogFile;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Functions.Services.BlobStorageClients;
using Atlas.Functions.Settings;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.SearchTracking.Common.Dispatchers;
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
    /// Safe under concurrent invocation. A finalisation lease is taken via
    /// <see cref="IParallelMatchPredictionRepository.TryClaimRunForFinalisation"/> before any work begins, and the
    /// run is only marked finalised (via <see cref="IParallelMatchPredictionRepository.MarkRunFinalised"/>) once the
    /// whole pipeline has succeeded. If this method fails part-way through, the run is <em>not</em> marked finalised,
    /// so it becomes eligible for re-Finalisation again once the lease lapses (rather than being abandoned).
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
    private readonly int parallelMpaBatchSize;
    private readonly TimeSpan finalisationLeaseDuration;

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
        parallelMpaBatchSize = orchestrationSettings.Value.ParallelMpaBatchSize;

        finalisationLeaseDuration = TimeSpan.FromMinutes(orchestrationSettings.Value.ParallelFinalisationLeaseDurationMinutes);
    }

    public async Task CompleteRun(int runId)
    {
        var runResults = await repository.GetRunWithResults(runId);
        if (runResults is null)
        {
            logger.SendTrace(
                $"Parallel match prediction run with id {runId} not found; skipping finalisation.",
                LogLevel.Warn);
            return;
        }
        var run = runResults.Run;
        var resultsLocations = runResults.MergedResultLocations;

        // Take an exclusive, time-bound lease before doing any work. Only one finaliser wins; others bail out.
        // Crucially the run is NOT marked finalised here — that happens only after the whole pipeline succeeds,
        // so a crash mid-pipeline leaves the run recoverable once the lease lapses.
        var leaseOwner = Guid.NewGuid();
        if (!await repository.TryClaimRunForFinalisation(runId, leaseOwner, DateTime.UtcNow, finalisationLeaseDuration))
        {
            logger.SendTrace(
                $"Parallel match prediction run with id {runId} is already finalised or being finalised by another instance; skipping.",
                LogLevel.Info);
            return;
        }

        var trackingSearchIdentifier = run.IsRepeatSearch
            ? run.RepeatSearchIdentifier!.Value
            : run.SearchIdentifier;
        var originalSearchIdentifier = run.IsRepeatSearch
            ? run.SearchIdentifier
            : (Guid?)null;

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
                : await CombineSearchResults(
                    resultSet.SearchRequestId,
                    matchingResultsSummary.Results,
                    resultsLocations
                )
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
                DonorsPerBatch: parallelMpaBatchSize,
                TotalNumberOfBatches: run.TotalBatchCount
            )
        );

        // Only now — after the entire pipeline has succeeded — mark the run finalised. Until this point a
        // failure leaves the run un-Finalised so it can be retried once the lease lapses.
        await repository.MarkRunFinalised(runId, leaseOwner, DateTime.UtcNow);
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
            var currentSearchResults = await CombineSearchResults(
                searchRequestId, matchingAlgorithmResults, matchPredictionResultLocationsForCurrentDonors
            );

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

    private async Task<IEnumerable<SearchResult>> CombineSearchResults(
        string searchRequestId,
        IEnumerable<MatchingAlgorithmResult> matchingResults,
        IReadOnlyDictionary<int, string> matchPredictionResultLocations) =>
        await resultsCombiner.CombineResults(searchRequestId, matchingResults, matchPredictionResultLocations);
}