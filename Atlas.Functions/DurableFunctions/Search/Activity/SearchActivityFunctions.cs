using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.DonorImport.ExternalInterface;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.Functions.Models;
using Atlas.Functions.Services;
using Atlas.Functions.Services.BlobStorageClients;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
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

        public SearchActivityFunctions(
            // Donor Import services
            IDonorReader donorReader,
            // Match Prediction services
            IMatchPredictionAlgorithm matchPredictionAlgorithm,
            IMatchPredictionInputBuilder matchPredictionInputBuilder,
            ISearchCompletionMessageSender searchCompletionMessageSender,
            IMatchingResultsDownloader matchingResultsDownloader,
            IResultsUploader searchResultsBlobUploader,
            IResultsCombiner resultsCombiner)
        {
            this.donorReader = donorReader;
            this.matchPredictionAlgorithm = matchPredictionAlgorithm;
            this.matchPredictionInputBuilder = matchPredictionInputBuilder;
            this.searchCompletionMessageSender = searchCompletionMessageSender;
            this.matchingResultsDownloader = matchingResultsDownloader;
            this.searchResultsBlobUploader = searchResultsBlobUploader;
            this.resultsCombiner = resultsCombiner;
        }

        [FunctionName(nameof(DownloadMatchingAlgorithmResults))]
        public async Task<TimedResultSet<MatchingAlgorithmResultSet>> DownloadMatchingAlgorithmResults(
            [ActivityTrigger] MatchingResultsNotification matchingResultsNotification)
        {
            var results = await matchingResultsDownloader.Download(matchingResultsNotification.BlobStorageResultsFileName);
            return new TimedResultSet<MatchingAlgorithmResultSet>
            {
                ElapsedTime = matchingResultsNotification.ElapsedTime,
                ResultSet = results
            };
        }

        [FunctionName(nameof(FetchDonorInformation))]
        public async Task<TimedResultSet<IDictionary<int, Donor>>> FetchDonorInformation([ActivityTrigger] IEnumerable<int> donorIds)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var donorInfo = await donorReader.GetDonors(donorIds);

            return new TimedResultSet<IDictionary<int, Donor>>
            {
                ElapsedTime = stopwatch.Elapsed,
                FinishedTimeUtc = DateTime.UtcNow,
                ResultSet = donorInfo,
            };
        }

        [FunctionName(nameof(BuildMatchPredictionInputs))]
        public async Task<IEnumerable<MultipleDonorMatchProbabilityInput>> BuildMatchPredictionInputs(
            [ActivityTrigger] MatchPredictionInputParameters matchPredictionInputParameters
        )
        {
            return matchPredictionInputBuilder.BuildMatchPredictionInputs(matchPredictionInputParameters);
        }

        [FunctionName(nameof(RunMatchPrediction))]
        public async Task<IReadOnlyDictionary<int, string>> RunMatchPrediction([ActivityTrigger] MultipleDonorMatchProbabilityInput matchProbabilityInput)
        {
            return await matchPredictionAlgorithm.RunMatchPredictionAlgorithmBatch(matchProbabilityInput);
        }

        [FunctionName(nameof(PersistSearchResults))]
        public async Task PersistSearchResults([ActivityTrigger] PersistSearchResultsParameters persistSearchResultsParameters)
        {
            var resultSet = await resultsCombiner.CombineResults(persistSearchResultsParameters);
            await searchResultsBlobUploader.UploadResults(resultSet);
            await searchCompletionMessageSender.PublishResultsMessage(resultSet, persistSearchResultsParameters.SearchInitiated);
        }
        
        [FunctionName(nameof(SendFailureNotification))]
        public async Task SendFailureNotification([ActivityTrigger] (string, string) failureInfo)
        {
            var (searchRequestId, failedStage) = failureInfo;

            await searchCompletionMessageSender.PublishFailureMessage(
                searchRequestId,
                $"Search failed at stage: {failedStage}. See Application Insights for failure details."
            );
        }

        /// <summary>
        /// Parameters wrapped in single object as Azure Activity functions may only have one parameter.
        /// </summary>
        public class PersistSearchResultsParameters
        {
            public TimedResultSet<MatchingAlgorithmResultSet> MatchingAlgorithmResultSet { get; set; }

            /// <summary>
            /// Keyed by ATLAS Donor ID
            /// </summary>
            public TimedResultSet<IReadOnlyDictionary<int, string>> MatchPredictionResultLocations { get; set; }

            /// <summary>
            /// Keyed by ATLAS ID
            /// </summary>
            public IDictionary<int, Donor> DonorInformation { get; set; }

            /// <summary>
            /// The time the *orchestration function* was initiated. Used to calculate an overall search time for Atlas search requests.
            /// </summary>
            public DateTime SearchInitiated { get; set; }
        }
    }
}