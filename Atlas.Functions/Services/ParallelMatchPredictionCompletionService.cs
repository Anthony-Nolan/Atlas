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
using Atlas.Functions.Models;
using Atlas.Functions.Services.BlobStorageClients;
using Atlas.Functions.Settings;
using Atlas.SearchTracking.Data.Models;
using Microsoft.Extensions.Options;

namespace Atlas.Functions.Services;

public interface IParallelMatchPredictionCompletionService
{
    /// <summary>
    /// Performs the final persistence pipeline for a completed parallel match-prediction search:
    /// combines MPA results with matching results, uploads to blob storage, sends the completion
    /// notification, uploads the search log, and emits the search-tracking event.
    /// <param name="metadata">Metadata for the parallel match-prediction search, including the original search request and tracking identifiers.
    /// Expected to have parent <see cref="SearchRequestMatchPrediction"/> and <see cref="SearchRequest"/></param>
    /// </summary>
    Task Complete(
        SearchRequestParallelMatchPredictionMetadata metadata,
        IReadOnlyDictionary<int, string> mergedMatchPredictionResultLocations);
}

public class ParallelMatchPredictionCompletionService : IParallelMatchPredictionCompletionService
{
    private readonly IMatchingResultsDownloader matchingResultsDownloader;
    private readonly IResultsCombiner resultsCombiner;
    private readonly ISearchResultsBlobStorageClient searchResultsBlobUploader;
    private readonly ISearchCompletionMessageSender searchCompletionMessageSender;
    private readonly IMatchPredictionSearchTrackingDispatcher matchPredictionSearchTrackingDispatcher;
    private readonly IAtlasLogger logger;
    private readonly AzureStorageSettings azureStorageSettings;
    private readonly int parallelMpaBatchSize;

    public ParallelMatchPredictionCompletionService(
        IMatchingResultsDownloader matchingResultsDownloader,
        IResultsCombiner resultsCombiner,
        ISearchResultsBlobStorageClient searchResultsBlobUploader,
        ISearchCompletionMessageSender searchCompletionMessageSender,
        IMatchPredictionSearchTrackingDispatcher matchPredictionSearchTrackingDispatcher,
        ISearchLogger<SearchLoggingContext> logger,
        IOptions<AzureStorageSettings> azureStorageSettings,
        IOptions<OrchestrationSettings> orchestrationSettings)
    {
        this.matchingResultsDownloader = matchingResultsDownloader;
        this.resultsCombiner = resultsCombiner;
        this.searchResultsBlobUploader = searchResultsBlobUploader;
        this.searchCompletionMessageSender = searchCompletionMessageSender;
        this.matchPredictionSearchTrackingDispatcher = matchPredictionSearchTrackingDispatcher;
        this.logger = logger;
        this.azureStorageSettings = azureStorageSettings.Value;
        parallelMpaBatchSize = orchestrationSettings.Value.ParallelMpaBatchSize;
    }

    public async Task Complete(
        SearchRequestParallelMatchPredictionMetadata metadata,
        IReadOnlyDictionary<int, string> mergedMatchPredictionResultLocations)
    {
        var trackingSearchIdentifier = metadata.IsRepeatSearch
            ? metadata.RepeatSearchIdentifier!.Value
            : metadata.SearchIdentifier;
        var originalSearchIdentifier = metadata.IsRepeatSearch
            ? metadata.SearchIdentifier
            : (Guid?)null;

        await matchPredictionSearchTrackingDispatcher.ProcessPersistingResultsStarted(
            trackingSearchIdentifier, originalSearchIdentifier
        );

        var matchingResultsSummary = await logger.RunTimedAsync(
            "Download matching results summary",
            async () => await matchingResultsDownloader.DownloadSummary(
                metadata.ResultsFileName,
                metadata.IsRepeatSearch
            )
        );

        // The parallel path does not provide match Prediction time, so set to zero
        var resultSet = resultsCombiner.BuildResultsSummary(
            matchingResultsSummary, TimeSpan.Zero, metadata.MatchingAlgorithmElapsedTime
        );

        resultSet.BlobStorageContainerName = resultSet.IsRepeatSearchSet
            ? azureStorageSettings.RepeatSearchResultsBlobContainer
            : azureStorageSettings.SearchResultsBlobContainer;
        resultSet.BatchedResult = metadata.ResultsBatched && azureStorageSettings.ShouldBatchResults;

        resultSet.Results = await logger.RunTimedAsync("Combining search results", async () =>
            metadata.ResultsBatched
                ? await ProcessBatchedSearchResults(
                    resultSet.SearchRequestId,
                    metadata.IsRepeatSearch,
                    mergedMatchPredictionResultLocations,
                    metadata.BatchFolderName,
                    resultSet.BlobStorageContainerName,
                    azureStorageSettings.ShouldBatchResults
                )
                : await ProcessSearchResults(
                    resultSet.SearchRequestId,
                    matchingResultsSummary.Results,
                    mergedMatchPredictionResultLocations
                )
        );

        await searchResultsBlobUploader.UploadResults(
            resultSet, resultSet.BlobStorageContainerName, resultSet.ResultsFileName
        );
        await matchPredictionSearchTrackingDispatcher.ProcessPersistingResultsEnded(
            trackingSearchIdentifier, originalSearchIdentifier
        );
        await searchCompletionMessageSender.PublishResultsMessage(
            resultSet, metadata.SearchInitiatedTimeUtc, metadata.BatchFolderName
        );
        await matchPredictionSearchTrackingDispatcher.ProcessResultsSent(
            trackingSearchIdentifier, originalSearchIdentifier
        );

        // Upload search log (SearchRequest is not available on the parallel path – logged as null)
        try
        {
            var searchLog = new SearchLog
            {
                SearchRequestId = metadata.SearchIdentifier.ToString(),
                WasSuccessful = true,
                SearchRequest = null,
                RequestPerformanceMetrics = new RequestPerformanceMetrics
                {
                    InitiationTime = metadata.SearchInitiatedTimeUtc,
                    StartTime = metadata.SearchInitiatedTimeUtc,
                    CompletionTime = DateTime.UtcNow,
                }
            };
            await searchResultsBlobUploader.UploadResults(
                searchLog,
                azureStorageSettings.SearchResultsBlobContainer,
                $"{metadata.SearchIdentifier}-log.json"
            );
        }
        catch
        {
            logger.SendTrace(
                $"Failed to write performance log file for search with id {metadata.SearchIdentifier}.",
                LogLevel.Error
            );
        }

        // Send match prediction process completed event
        await matchPredictionSearchTrackingDispatcher.ProcessCompleted(
            new MatchPredictionProcessCompletedParameters
            {
                SearchIdentifier = trackingSearchIdentifier,
                OriginalSearchIdentifier = originalSearchIdentifier,
                FailureInfo = null,
                DonorsPerBatch = parallelMpaBatchSize,
                TotalNumberOfBatches = null
            }
        );
    }

    private async Task<IEnumerable<SearchResult>> ProcessBatchedSearchResults(
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
                matchPredictionResultLocations.Where(l => donorIds.Contains(l.Key)).ToDictionary();
            var currentSearchResults = await ProcessSearchResults(
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

    private async Task<IEnumerable<SearchResult>> ProcessSearchResults(
        string searchRequestId,
        IEnumerable<MatchingAlgorithmResult> matchingResults,
        IReadOnlyDictionary<int, string> matchPredictionResultLocations) =>
        await resultsCombiner.CombineResults(searchRequestId, matchingResults, matchPredictionResultLocations);
}