using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.AzureStorage.Blob;
using Atlas.DonorImport.ExternalInterface;
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
        // Donor Import services
        private readonly IDonorReader donorReader;

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
        private readonly AzureStorageSettings azureStorageSettings;

        public SearchActivityFunctions(
            // Donor Import services
            IDonorReader donorReader,
            // Match Prediction services
            IMatchPredictionAlgorithm matchPredictionAlgorithm,
            IMatchPredictionInputBuilder matchPredictionInputBuilder,
            ISearchCompletionMessageSender searchCompletionMessageSender,
            IMatchingResultsDownloader matchingResultsDownloader,
            ISearchResultsBlobStorageClient searchResultsBlobUploader,
            IResultsCombiner resultsCombiner,
            ILogger logger,
            IMatchPredictionRequestBlobClient matchPredictionRequestBlobClient,
            IOptions<AzureStorageSettings> azureStorageSettings)
        {
            this.donorReader = donorReader;
            this.matchPredictionAlgorithm = matchPredictionAlgorithm;
            this.matchPredictionInputBuilder = matchPredictionInputBuilder;
            this.searchCompletionMessageSender = searchCompletionMessageSender;
            this.matchingResultsDownloader = matchingResultsDownloader;
            this.searchResultsBlobUploader = searchResultsBlobUploader;
            this.resultsCombiner = resultsCombiner;
            this.logger = logger;
            this.matchPredictionRequestBlobClient = matchPredictionRequestBlobClient;
            this.azureStorageSettings = azureStorageSettings.Value;
        }

        [FunctionName(nameof(PrepareMatchPredictionBatches))]
        public async Task<TimedResultSet<IList<string>>> PrepareMatchPredictionBatches(
            [ActivityTrigger] MatchingResultsNotification matchingResultsNotification)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var matchingResults = await logger.RunTimedAsync("Download matching results", async () =>
                await matchingResultsDownloader.Download(
                    matchingResultsNotification.ResultsFileName,
                    matchingResultsNotification.IsRepeatSearch,
                    matchingResultsNotification.ResultsBatched ? matchingResultsNotification.BatchFolderName : null)
            );

            var donorInfo = await logger.RunTimedAsync("Fetch donor data", async () =>
                await donorReader.GetDonors(matchingResults.Results.Select(r => r.AtlasDonorId))
            );

            var matchPredictionInputs = logger.RunTimed("Build Match Prediction Inputs", () =>
                matchPredictionInputBuilder.BuildMatchPredictionInputs(new MatchPredictionInputParameters
                {
                    DonorDictionary = donorInfo,
                    SearchRequest = matchingResultsNotification.SearchRequest,
                    MatchingAlgorithmResults = matchingResults
                })
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
            var matchingResultsNotification = parameters.MatchingResultsNotification;

            var matchingResults = await logger.RunTimedAsync("Download matching results", async () =>
                await matchingResultsDownloader.Download(
                    matchingResultsNotification.ResultsFileName,
                    matchingResultsNotification.IsRepeatSearch,
                    matchingResultsNotification.ResultsBatched ? matchingResultsNotification.BatchFolderName : null)
            );

            // TODO: ATLAS-965 - use the lookup in matching to populate this and avoid a second SQL fetch
            var donorInfo = await logger.RunTimedAsync("Fetch donor data", async () =>
                await donorReader.GetDonors(matchingResults.Results.Select(r => r.AtlasDonorId))
            );

            var resultSet = await resultsCombiner.CombineResults(
                matchingResults,
                donorInfo,
                parameters.MatchPredictionResultLocations,
                parameters.MatchingResultsNotification.ElapsedTime
            );

            resultSet.BlobStorageContainerName = resultSet.IsRepeatSearchSet ? azureStorageSettings.RepeatSearchResultsBlobContainer : azureStorageSettings.SearchResultsBlobContainer;
            resultSet.BatchedResult = azureStorageSettings.ShouldBatchResults;

            await searchResultsBlobUploader.UploadResults(resultSet, matchingResultsNotification.BatchFolderName);
            await searchCompletionMessageSender.PublishResultsMessage(resultSet, parameters.SearchInitiated);
        }

        [FunctionName(nameof(SendFailureNotification))]
        public async Task SendFailureNotification([ActivityTrigger] FailureNotificationRequestInfo requestInfo)
        {
            await searchCompletionMessageSender.PublishFailureMessage(requestInfo);
        }
    }
}