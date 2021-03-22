using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.DonorImport.ExternalInterface;
using Atlas.Functions.Models;
using Atlas.Functions.Services;
using Atlas.Functions.Services.BlobStorageClients;
using Atlas.MatchPrediction.ExternalInterface;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

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
        private readonly IResultsUploader searchResultsBlobUploader;
        private readonly IResultsCombiner resultsCombiner;
        private readonly ILogger logger;
        private readonly IMatchPredictionRequestBlobClient matchPredictionRequestBlobClient;

        public SearchActivityFunctions(
            // Donor Import services
            IDonorReader donorReader,
            // Match Prediction services
            IMatchPredictionAlgorithm matchPredictionAlgorithm,
            IMatchPredictionInputBuilder matchPredictionInputBuilder,
            ISearchCompletionMessageSender searchCompletionMessageSender,
            IMatchingResultsDownloader matchingResultsDownloader,
            IResultsUploader searchResultsBlobUploader,
            IResultsCombiner resultsCombiner,
            ILogger logger,
            IMatchPredictionRequestBlobClient matchPredictionRequestBlobClient)
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
        }

        [FunctionName(nameof(PrepareMatchPredictionBatches))]
        public async Task<TimedResultSet<IList<string>>> PrepareMatchPredictionBatches(
            [ActivityTrigger] MatchingResultsNotification matchingResultsNotification)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var matchingResults = await logger.RunTimedAsync("Download matching results", async () =>
                await matchingResultsDownloader.Download(matchingResultsNotification.ResultsFileName, matchingResultsNotification.IsRepeatSearch)
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
                var matchPredictionRequestFileNames = new List<string>();
                foreach (var matchPredictionInput in matchPredictionInputs)
                {
                    var fileName = await matchPredictionRequestBlobClient.UploadBatchRequest(matchingResultsNotification.SearchRequestId,
                        matchPredictionInput);
                    matchPredictionRequestFileNames.Add(fileName);
                }

                return new TimedResultSet<IList<string>>
                {
                    ElapsedTime = stopwatch.Elapsed,
                    ResultSet = matchPredictionRequestFileNames,
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
        public async Task PersistSearchResults([ActivityTrigger] PersistSearchResultsParameters persistSearchResultsParameters)
        {
            var matchingResultsNotification = persistSearchResultsParameters.MatchingResultsNotification;

            var matchingResults = await logger.RunTimedAsync("Download matching results", async () =>
                await matchingResultsDownloader.Download(matchingResultsNotification.ResultsFileName, matchingResultsNotification.IsRepeatSearch)
            );

            // TODO: ATLAS-965 - use the lookup in matching to populate this and avoid a second SQL fetch
            var donorInfo = await logger.RunTimedAsync("Fetch donor data", async () =>
                await donorReader.GetDonors(matchingResults.Results.Select(r => r.AtlasDonorId))
            );

            var resultSet = await resultsCombiner.CombineResults(
                matchingResults,
                donorInfo,
                persistSearchResultsParameters.MatchPredictionResultLocations,
                persistSearchResultsParameters.MatchingResultsNotification.ElapsedTime
            );
            
            await searchResultsBlobUploader.UploadResults(resultSet);
            await searchCompletionMessageSender.PublishResultsMessage(resultSet, persistSearchResultsParameters.SearchInitiated);
        }

        [FunctionName(nameof(SendFailureNotification))]
        public async Task SendFailureNotification([ActivityTrigger] (string, string, string) failureInfo)
        {
            var (searchRequestId, failedStage, repeatSearchId) = failureInfo;

            await searchCompletionMessageSender.PublishFailureMessage(
                searchRequestId,
                repeatSearchId,
                $"Search failed at stage: {failedStage}. See Application Insights for failure details."
            );
        }

        /// <summary>
        /// Parameters wrapped in single object as Azure Activity functions may only have one parameter.
        /// </summary>
        public class PersistSearchResultsParameters
        {
            public MatchingResultsNotification MatchingResultsNotification { get; set; }

            /// <summary>
            /// Keyed by ATLAS Donor ID
            /// </summary>
            public TimedResultSet<IReadOnlyDictionary<int, string>> MatchPredictionResultLocations { get; set; }

            /// <summary>
            /// The time the *orchestration function* was initiated. Used to calculate an overall search time for Atlas search requests.
            /// </summary>
            public DateTime SearchInitiated { get; set; }
        }
    }
}