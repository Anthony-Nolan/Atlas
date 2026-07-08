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
using Atlas.Common.ServiceBus;
using Atlas.Functions.Models;
using Atlas.Functions.Services;
using Atlas.Functions.Services.BlobStorageClients;
using Atlas.Functions.Settings;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.SearchTracking.Common.Dispatchers;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Microsoft.Azure.Functions.Worker;
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
        private readonly IAtlasLogger logger;
        private readonly IMatchPredictionRequestBlobClient matchPredictionRequestBlobClient;
        private readonly SearchLoggingContext loggingContext;
        private readonly AzureStorageSettings azureStorageSettings;
        private readonly IMatchPredictionSearchTrackingDispatcher matchPredictionSearchTrackingDispatcher;

        private readonly IMessageBatchPublisher<ParallelMatchPredictionBatchRequest> parallelBatchPublisher;
        private readonly IParallelMatchPredictionRepository parallelMatchPredictionRepository;
        private readonly int parallelMatchPredictionBatchSize;

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
            IMatchPredictionSearchTrackingDispatcher matchPredictionSearchTrackingDispatcher,
            IMessageBatchPublisher<ParallelMatchPredictionBatchRequest> parallelBatchPublisher,
            IParallelMatchPredictionRepository parallelMatchPredictionRepository,
            IOptions<AzureStorageSettings> azureStorageSettings,
            IOptions<OrchestrationSettings> orchestrationSettings,
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
            this.matchPredictionSearchTrackingDispatcher = matchPredictionSearchTrackingDispatcher;
            this.parallelBatchPublisher = parallelBatchPublisher;
            this.parallelMatchPredictionRepository = parallelMatchPredictionRepository;
            this.loggingContext = loggingContext;
            this.azureStorageSettings = azureStorageSettings.Value;
            parallelMatchPredictionBatchSize = orchestrationSettings.Value.ParallelMatchPredictionBatchSize;
        }

        [Function(nameof(PrepareMatchPredictionBatches))]
        public async Task<TimedResultSet<IList<string>>> PrepareMatchPredictionBatches(
            [ActivityTrigger] MatchingResultsNotification matchingResultsNotification)
        {
            InitializeLoggingContext(matchingResultsNotification.SearchRequestId);

            var trackingSearchIdentifier = matchingResultsNotification.IsRepeatSearch
                ? new Guid(matchingResultsNotification.RepeatSearchRequestId)
                : new Guid(matchingResultsNotification.SearchRequestId);
            var originalSearchIdentifier = matchingResultsNotification.IsRepeatSearch
                ? new Guid(matchingResultsNotification.SearchRequestId)
                : (Guid?)null;

            await matchPredictionSearchTrackingDispatcher.ProcessPrepareBatchesStarted(trackingSearchIdentifier, originalSearchIdentifier);

            var timedResultSet = new TimedResultSet<IList<string>>();
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

                timedResultSet = new TimedResultSet<IList<string>>
                {
                    ElapsedTime = stopwatch.Elapsed,
                    ResultSet = matchPredictionRequestFileNames.ToList(),
                    FinishedTimeUtc = DateTime.UtcNow
                };
            }

            await matchPredictionSearchTrackingDispatcher.ProcessPrepareBatchesEnded(trackingSearchIdentifier, originalSearchIdentifier);
            return timedResultSet;
        }

        /// <summary>
        /// Parallel match-prediction path: downloads matching results, builds batched inputs using
        /// <see cref="OrchestrationSettings.ParallelMatchPredictionBatchSize"/>,
        /// uploads blobs, then creates the run record and pre-creates one batch row per blob in the repository
        /// (ensuring <c>BatchSequenceNumber</c> 0 … N−1 matches the order in <c>blobLocations</c>), and finally
        /// publishes one <see cref="ParallelMatchPredictionBatchRequest"/> message per blob to
        /// <c>parallel-match-prediction-requests</c>.  The ACA Worker processes each batch and publishes results
        /// to <c>parallel-match-prediction-results</c>; the aggregator function handles final persistence.
        /// </summary>
        [Function(nameof(PrepareAndDispatchParallelMatchPredictionBatches))]
        public async Task PrepareAndDispatchParallelMatchPredictionBatches(
            [ActivityTrigger] PrepareAndDispatchParallelMatchPredictionBatchesParameters parameters)
        {
            var matchingResultsNotification = parameters.MatchingResultsNotification;
            InitializeLoggingContext(matchingResultsNotification.SearchRequestId);

            var trackingSearchIdentifier = matchingResultsNotification.IsRepeatSearch
                ? new Guid(matchingResultsNotification.RepeatSearchRequestId)
                : new Guid(matchingResultsNotification.SearchRequestId);
            var originalSearchIdentifier = matchingResultsNotification.IsRepeatSearch
                ? new Guid(matchingResultsNotification.SearchRequestId)
                : (Guid?)null;

            await matchPredictionSearchTrackingDispatcher.ProcessPrepareBatchesStarted(trackingSearchIdentifier, originalSearchIdentifier);

            var matchingResults = await logger.RunTimedAsync("Download matching results", async () =>
                await matchingResultsDownloader.Download(
                    matchingResultsNotification.ResultsFileName,
                    matchingResultsNotification.IsRepeatSearch,
                    matchingResultsNotification.ResultsBatched ? matchingResultsNotification.BatchFolderName : null)
            );

            var matchPredictionInputs = logger.RunTimed("Build Parallel Match Prediction Inputs", () =>
                matchPredictionInputBuilder.BuildMatchPredictionInputs(matchingResults, parallelMatchPredictionBatchSize)
            );

            IList<string> blobLocations;
            using (logger.RunTimed("Uploading parallel match prediction requests"))
            {
                var fileNames = await matchPredictionRequestBlobClient.UploadMatchProbabilityRequests(
                    matchingResultsNotification.SearchRequestId, matchPredictionInputs);
                blobLocations = fileNames.ToList();
            }

            await matchPredictionSearchTrackingDispatcher.ProcessPrepareBatchesEnded(trackingSearchIdentifier, originalSearchIdentifier);

            var runResult = await parallelMatchPredictionRepository.CreateRun(
                new CreateParallelMatchPredictionRunInfo(
                    SearchIdentifier: new Guid(matchingResultsNotification.SearchRequestId),
                    IsRepeatSearch: matchingResultsNotification.IsRepeatSearch,
                    RepeatSearchIdentifier: matchingResultsNotification.RepeatSearchRequestId != null
                        ? new Guid(matchingResultsNotification.RepeatSearchRequestId)
                        : null,
                    ResultsFileName: matchingResultsNotification.ResultsFileName,
                    ResultsBatched: matchingResultsNotification.ResultsBatched,
                    BatchFolderName: matchingResultsNotification.BatchFolderName,
                    MatchingAlgorithmElapsedTime: matchingResultsNotification.ElapsedTime,
                    SearchInitiatedTimeUtc: parameters.SearchInitiatedTimeUtc,
                    TotalBatchCount: blobLocations.Count
                )
            );

            var batchRequests = blobLocations.Select((location, index) => new ParallelMatchPredictionBatchRequest
            {
                BlobLocation = location,
                SearchRequestId = matchingResultsNotification.SearchRequestId,
                IsRepeatSearch = matchingResultsNotification.IsRepeatSearch,
                RepeatSearchRequestId = matchingResultsNotification.IsRepeatSearch
                    ? matchingResultsNotification.RepeatSearchRequestId
                    : null,
                ParallelRunId = runResult.RunId,
                BatchId = runResult.BatchIdsBySequence[index],
                BatchSequenceNumber = index,
            });

            await parallelBatchPublisher.BatchPublish(batchRequests);
        }

        [Function(nameof(RunMatchPredictionBatch))]
        public async Task<IReadOnlyDictionary<int, string>> RunMatchPredictionBatch([ActivityTrigger] string requestLocation)
        {
            var matchProbabilityInput = await matchPredictionRequestBlobClient.DownloadBatchRequest(requestLocation);
            InitializeLoggingContext(matchProbabilityInput.SearchRequestId);
            return await matchPredictionAlgorithm.RunMatchPredictionAlgorithmBatch(matchProbabilityInput);
        }

        [Function(nameof(PersistSearchResults))]
        public async Task PersistSearchResults([ActivityTrigger] PersistSearchResultsFunctionParameters parameters)
        {
            InitializeLoggingContext(parameters.MatchingResultsNotification.SearchRequestId);
            var matchingResultsNotification = parameters.MatchingResultsNotification;
            var trackingSearchIdentifier = matchingResultsNotification.IsRepeatSearch
                ? new Guid(matchingResultsNotification.RepeatSearchRequestId)
                : new Guid(matchingResultsNotification.SearchRequestId);
            var originalSearchIdentifier = matchingResultsNotification.IsRepeatSearch
                ? new Guid(matchingResultsNotification.SearchRequestId)
                : (Guid?)null;

            await matchPredictionSearchTrackingDispatcher.ProcessPersistingResultsStarted(trackingSearchIdentifier, originalSearchIdentifier);

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
            await matchPredictionSearchTrackingDispatcher.ProcessPersistingResultsEnded(trackingSearchIdentifier, originalSearchIdentifier);
            await searchCompletionMessageSender.PublishResultsMessage(resultSet, parameters.SearchInitiated, matchingResultsNotification.BatchFolderName);
            await matchPredictionSearchTrackingDispatcher.ProcessResultsSent(trackingSearchIdentifier, originalSearchIdentifier);
        }

        [Function(nameof(SendFailureNotification))]
        public async Task SendFailureNotification([ActivityTrigger] SendFailureNotificationParameters parameters)
        {
            InitializeLoggingContext(parameters.SearchRequestId);
            var trackingSearchIdentifier = new Guid(parameters.RepeatSearchRequestId ?? parameters.SearchRequestId);
            var originalSearchIdentifier = parameters.RepeatSearchRequestId != null
                ? new Guid(parameters.SearchRequestId)
                : (Guid?)null;
            await searchCompletionMessageSender.PublishFailureMessage(parameters);
            await matchPredictionSearchTrackingDispatcher.ProcessResultsSent(trackingSearchIdentifier, originalSearchIdentifier);
        }

        [Function(nameof(UploadSearchLog))]
        public async Task UploadSearchLog([ActivityTrigger] SearchLog searchLog)
        {
            InitializeLoggingContext(searchLog.SearchRequestId);
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

        [Function(nameof(SendMatchPredictionProcessInitiated))]
        public async Task SendMatchPredictionProcessInitiated([ActivityTrigger] MatchPredictionProcessInitiatedParameters parameters)
        {
            await matchPredictionSearchTrackingDispatcher.ProcessInitiation(
                parameters.SearchIdentifier, parameters.OriginalSearchIdentifier, parameters.InitiationTimeUtc, parameters.IsParallelMatchPrediction);
        }

        [Function(nameof(SendMatchPredictionBatchProcessingStarted))]
        public async Task SendMatchPredictionBatchProcessingStarted([ActivityTrigger] MatchPredictionSearchIdentifiers parameters)
        {
            await matchPredictionSearchTrackingDispatcher.ProcessRunningBatchesStarted(parameters.SearchIdentifier, parameters.OriginalSearchIdentifier);
        }

        [Function(nameof(SendMatchPredictionBatchProcessingEnded))]
        public async Task SendMatchPredictionBatchProcessingEnded([ActivityTrigger] MatchPredictionSearchIdentifiers parameters)
        {
            await matchPredictionSearchTrackingDispatcher.ProcessRunningBatchesEnded(parameters.SearchIdentifier, parameters.OriginalSearchIdentifier);
        }

        [Function(nameof(SendMatchPredictionProcessCompleted))]
        public async Task SendMatchPredictionProcessCompleted([ActivityTrigger] MatchPredictionProcessCompletedParameters parameters)
        {
            await matchPredictionSearchTrackingDispatcher.ProcessCompleted(
                (parameters.SearchIdentifier,
                    parameters.OriginalSearchIdentifier,
                    parameters.IsSuccessful,
                    parameters.FailureInfo,
                    parameters.DonorsPerBatch,
                    parameters.TotalNumberOfBatches)
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