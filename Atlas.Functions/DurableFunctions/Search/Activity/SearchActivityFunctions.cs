using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.LogFile;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Common.Utils.Extensions;
using Atlas.Functions.Models;
using Atlas.Functions.Services;
using Atlas.Functions.Services.BlobStorageClients;
using Atlas.Functions.Settings;
using Atlas.MatchPrediction.ExternalInterface;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Options;

namespace Atlas.Functions.DurableFunctions.Search.Activity
{
    public class SearchActivityFunctions
    {
        // Match Prediction services
        private readonly IMatchPredictionAlgorithm matchPredictionAlgorithm;

        // Atlas.Functions services
        private readonly IMatchPredictionInputBuilder matchPredictionInputBuilder;
        private readonly ISearchCompletionMessageSender searchCompletionMessageSender;
        private readonly IMatchingResultsDownloader matchingResultsDownloader;
        private readonly ISearchResultsBlobStorageClient searchResultsBlobUploader;
        private readonly IResultsCombiner resultsCombiner;
        private readonly ILogger logger;
        private readonly IMatchPredictionRequestBlobClient matchPredictionRequestBlobClient;
        private readonly SearchLoggingContext loggingContext;
        private readonly AzureStorageSettings azureStorageSettings;

        public SearchActivityFunctions(
            // Match Prediction services
            IMatchPredictionAlgorithm matchPredictionAlgorithm,
            IMatchPredictionInputBuilder matchPredictionInputBuilder,
            ISearchCompletionMessageSender searchCompletionMessageSender,
            IMatchingResultsDownloader matchingResultsDownloader,
            ISearchResultsBlobStorageClient searchResultsBlobUploader,
            IResultsCombiner resultsCombiner,
            ISearchLogger<SearchLoggingContext> logger,
            IMatchPredictionRequestBlobClient matchPredictionRequestBlobClient,
            IOptions<AzureStorageSettings> azureStorageSettings,
            SearchLoggingContext loggingContext)
        {
            this.matchPredictionAlgorithm = matchPredictionAlgorithm;
            this.matchPredictionInputBuilder = matchPredictionInputBuilder;
            this.searchCompletionMessageSender = searchCompletionMessageSender;
            this.matchingResultsDownloader = matchingResultsDownloader;
            this.searchResultsBlobUploader = searchResultsBlobUploader;
            this.resultsCombiner = resultsCombiner;
            this.logger = logger;
            this.matchPredictionRequestBlobClient = matchPredictionRequestBlobClient;
            this.loggingContext = loggingContext;
            this.azureStorageSettings = azureStorageSettings.Value;
        }

        [FunctionName(nameof(PrepareMatchPredictionBatches))]
        public async Task<TimedResultSet<IList<string>>> PrepareMatchPredictionBatches(
            [ActivityTrigger] MatchingResultsNotification matchingResultsNotification)
        {
            InitializeLoggingContext(matchingResultsNotification.SearchRequestId);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var matchingResults = await logger.RunTimedAsync("Download matching results", async () =>
                await matchingResultsDownloader.Download(
                    matchingResultsNotification.ResultsFileName,
                    matchingResultsNotification.IsRepeatSearch,
                    matchingResultsNotification.ResultsBatched ? matchingResultsNotification.BatchFolderName : null)
            );

            var matchPredictionInputs = logger.RunTimed("Build Match Prediction Inputs", () =>
                matchPredictionInputBuilder.BuildMatchPredictionInputs(matchingResults)
            );

            using (logger.RunTimed("Uploading match prediction requests"))
            {
                var matchPredictionRequestFileNames = await matchPredictionRequestBlobClient.UploadMatchProbabilityRequests(matchingResultsNotification.SearchRequestId, matchPredictionInputs);
                
                return new TimedResultSet<IList<string>>
                {
                    ElapsedTime = stopwatch.Elapsed,
                    ResultSet = matchPredictionRequestFileNames.ToList(),
                    FinishedTimeUtc = DateTime.UtcNow
                };
            }
        }

        [FunctionName(nameof(RunMatchPredictionBatch))]
        public async Task<IReadOnlyDictionary<int, string>> RunMatchPredictionBatch([ActivityTrigger] string requestLocation)
        {
            var matchProbabilityInput = await matchPredictionRequestBlobClient.DownloadBatchRequest(requestLocation);
            return await matchPredictionAlgorithm.RunMatchPredictionAlgorithmBatch(matchProbabilityInput);
        }

        [FunctionName(nameof(PersistSearchResults))]
        public async Task PersistSearchResults([ActivityTrigger] PersistSearchResultsFunctionParameters parameters)
        {
            InitializeLoggingContext(parameters.MatchingResultsNotification.SearchRequestId);

            var matchingResultsNotification = parameters.MatchingResultsNotification;

            var matchingResultsSummary = await logger.RunTimedAsync("Download matching results summary", async () =>
                await matchingResultsDownloader.DownloadSummary(
                    matchingResultsNotification.ResultsFileName,
                    matchingResultsNotification.IsRepeatSearch)
            );

            var resultSet = resultsCombiner.BuildResultsSummary(matchingResultsSummary, parameters.MatchPredictionResultLocations.ElapsedTime, parameters.MatchingResultsNotification.ElapsedTime);

            resultSet.BlobStorageContainerName = resultSet.IsRepeatSearchSet ? azureStorageSettings.RepeatSearchResultsBlobContainer : azureStorageSettings.SearchResultsBlobContainer;
            resultSet.BatchedResult = matchingResultsNotification.ResultsBatched && azureStorageSettings.ShouldBatchResults;

            resultSet.Results = await logger.RunTimedAsync("Combining search results", async () =>
                matchingResultsNotification.ResultsBatched
                ? await ProcessBatchedSearchResults(
                    resultSet.SearchRequestId,
                    matchingResultsNotification.IsRepeatSearch,
                    parameters.MatchPredictionResultLocations.ResultSet,
                    matchingResultsNotification.BatchFolderName,
                    resultSet.BlobStorageContainerName,
                    azureStorageSettings.ShouldBatchResults)
                : await ProcessSearchResults(resultSet.SearchRequestId, matchingResultsSummary.Results, parameters.MatchPredictionResultLocations.ResultSet)
            );

            await searchResultsBlobUploader.UploadResults(resultSet, resultSet.BlobStorageContainerName, resultSet.ResultsFileName);
            await searchCompletionMessageSender.PublishResultsMessage(resultSet, parameters.SearchInitiated, matchingResultsNotification.BatchFolderName);
        }

        [FunctionName(nameof(SendFailureNotification))]
        public async Task SendFailureNotification([ActivityTrigger] FailureNotificationRequestInfo requestInfo)
        {
            await searchCompletionMessageSender.PublishFailureMessage(requestInfo);
        }

        [FunctionName(nameof(UploadSearchLog))]
        public async Task UploadSearchLog([ActivityTrigger] SearchLog searchLog)
        {
            try
            {
                await searchResultsBlobUploader.UploadResults(searchLog, azureStorageSettings.SearchResultsBlobContainer,
                    $"{searchLog.SearchRequestId}-log.json");
            }
            catch
            {
                logger.SendTrace($"Failed to write performance log file for search with id {searchLog.SearchRequestId}.", LogLevel.Error);
            }
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
                var donorIds = matchingResults.Select(r => r.AtlasDonorId).ToList();
                var matchPredictionResultLocationsForCurrentDonors = matchPredictionResultLocations.Where(l => donorIds.Contains(l.Key)).ToDictionary();
                var currentSearchResults = await ProcessSearchResults(searchRequestId, matchingResults, matchPredictionResultLocationsForCurrentDonors);
                if (resultsShouldBeBatched)
                {
                    await searchResultsBlobUploader.UploadResults(currentSearchResults, blobStorageContainerName, $"{batchFolder}/{++batchNumber}.json");
                }
                else
                {
                    allSearchResults.AddRange(currentSearchResults);
                }
            }

            return allSearchResults;
        }

        private async Task<IEnumerable<SearchResult>> ProcessSearchResults(string searchRequestId, IEnumerable<MatchingAlgorithmResult> matchingResults, IReadOnlyDictionary<int, string> matchPredictionResultLocations) =>
            await resultsCombiner.CombineResults(searchRequestId, matchingResults, matchPredictionResultLocations);

        private void InitializeLoggingContext(string searchRequestId)
        {
            loggingContext.SearchRequestId = searchRequestId;
        }
    }
}