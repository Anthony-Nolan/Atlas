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
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Atlas.Functions.DurableFunctions.Search.Activity
{
    public class SearchActivityFunctions
    {
        // Matching Algorithm Services
        private readonly ISearchRunner searchRunner;

        // Donor Import services
        private readonly IDonorReader donorReader;

        // Match Prediction services
        private readonly IMatchPredictionAlgorithm matchPredictionAlgorithm;

        // Atlas.Functions services
        private readonly IResultsUploader searchResultsBlobUploader;
        private readonly IMatchPredictionInputBuilder matchPredictionInputBuilder;
        private readonly IResultsCombiner resultsCombiner;
        private readonly ISearchCompletionMessageSender searchCompletionMessageSender;
        private readonly IMatchingResultsDownloader matchingResultsDownloader;

        public SearchActivityFunctions(
            // Matching Algorithm Services
            ISearchRunner searchRunner,
            // Donor Import services
            IDonorReader donorReader,
            // Match Prediction services
            IMatchPredictionAlgorithm matchPredictionAlgorithm,
            // Atlas.Functions services
            IResultsUploader searchResultsBlobUploader,
            IMatchPredictionInputBuilder matchPredictionInputBuilder,
            IResultsCombiner resultsCombiner,
            ISearchCompletionMessageSender searchCompletionMessageSender,
            IMatchingResultsDownloader matchingResultsDownloader)
        {
            this.searchRunner = searchRunner;
            this.donorReader = donorReader;
            this.matchPredictionAlgorithm = matchPredictionAlgorithm;
            this.searchResultsBlobUploader = searchResultsBlobUploader;
            this.matchPredictionInputBuilder = matchPredictionInputBuilder;
            this.resultsCombiner = resultsCombiner;
            this.searchCompletionMessageSender = searchCompletionMessageSender;
            this.matchingResultsDownloader = matchingResultsDownloader;
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

        [FunctionName(nameof(RunMatchingAlgorithm))]
        public async Task<TimedResultSet<MatchingAlgorithmResultSet>> RunMatchingAlgorithm([ActivityTrigger] IdentifiedSearchRequest searchRequest)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var results = await searchRunner.RunSearch(searchRequest);

            return new TimedResultSet<MatchingAlgorithmResultSet>
            {
                ElapsedTime = stopwatch.Elapsed,
                FinishedTimeUtc = DateTime.UtcNow,
                ResultSet = results
            };
        }

        [FunctionName(nameof(FetchDonorInformation))]
        public async Task<TimedResultSet<IDictionary<int, Donor>>> FetchDonorInformation(
            [ActivityTrigger] Tuple<string, IEnumerable<int>> searchAndDonorIds)
        {
            var (_, donorIds) = searchAndDonorIds;
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
        public async Task<IReadOnlyDictionary<int, MatchProbabilityResponse>> RunMatchPrediction(
            [ActivityTrigger] MultipleDonorMatchProbabilityInput matchProbabilityInput)
        {
            return await matchPredictionAlgorithm.RunMatchPredictionAlgorithmBatch(matchProbabilityInput);
        }

        [FunctionName(nameof(SendFailureNotification))]
        public async Task SendFailureNotification([ActivityTrigger] (string, string) failureInfo)
        {
            var (requestId, failedStage) = failureInfo;

            await searchCompletionMessageSender.PublishFailureMessage(
                requestId,
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
            /// Keyed by ATLAS ID
            /// </summary>
            public TimedResultSet<Dictionary<int, MatchProbabilityResponse>> MatchPredictionResults { get; set; }

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