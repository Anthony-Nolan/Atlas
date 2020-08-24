using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.Utils.Extensions;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.Functions.DurableFunctions.Search.Activity;
using Atlas.Functions.Models;
using Atlas.Functions.Services;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Atlas.Functions.DurableFunctions.Search.Orchestration
{
    /// <summary>
    /// Note that orchestration triggered functions will be run multiple times per-request scope, so need to follow the code constraints
    /// as documented by Microsoft https://docs.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-code-constraints.
    /// Logging should be avoided in this function due to this.
    /// Any expensive or non-deterministic code should be called from an Activity function.
    /// </summary>
    // ReSharper disable once MemberCanBeInternal
    public static class SearchOrchestrationFunctions
    {
        private static readonly RetryOptions RetryOptions = new RetryOptions(TimeSpan.FromSeconds(5), 5) {BackoffCoefficient = 2};

        [FunctionName(nameof(SearchOrchestrator))]
        public static async Task<SearchOrchestrationOutput> SearchOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context
        )
        {
            var orchestrationInitiated = context.CurrentUtcDateTime;
            var notification = context.GetInput<MatchingResultsNotification>();
            var searchRequest = notification.SearchRequest;

            var timedSearchResults = await DownloadMatchingAlgorithmResults(context, notification);
            var searchResults = timedSearchResults.ResultSet;

            var timedDonorInformation = await FetchDonorInformation(context, searchResults);
            var donorInformation = timedDonorInformation.ResultSet;

            var matchPredictionResults = await RunMatchPredictionAlgorithm(context, searchRequest, searchResults, timedDonorInformation);
            await PersistSearchResults(context, timedSearchResults, matchPredictionResults, donorInformation, orchestrationInitiated);

            // "return" populates the "output" property on the status check GET endpoint set up by the durable functions framework
            return new SearchOrchestrationOutput
            {
                MatchingAlgorithmTime = timedSearchResults.ElapsedTime,
                MatchPredictionTime = matchPredictionResults.ElapsedTime,
                TotalSearchTime = context.CurrentUtcDateTime.Subtract(orchestrationInitiated),
                MatchingDonorCount = searchResults.ResultCount,
                MatchingResultFileName = searchResults.ResultsFileName,
                MatchingResultBlobContainer = searchResults.BlobStorageContainerName,
                HlaNomenclatureVersion = searchResults.HlaNomenclatureVersion
            };
        }

        private static async Task<TimedResultSet<MatchingAlgorithmResultSet>> DownloadMatchingAlgorithmResults(
            IDurableOrchestrationContext context,
            MatchingResultsNotification notification)
        {
            var matchingResults = await context.CallActivityWithRetryAsync<TimedResultSet<MatchingAlgorithmResultSet>>(
                nameof(SearchActivityFunctions.DownloadMatchingAlgorithmResults),
                RetryOptions,
                notification
            );

            context.SetCustomStatus(new OrchestrationStatus
            {
                LastCompletedStage = "MatchingAlgorithm",
                ElapsedTimeOfStage = notification.ElapsedTime,
            });

            return matchingResults;
        }

        private static async Task<TimedResultSet<Dictionary<int, MatchProbabilityResponse>>> RunMatchPredictionAlgorithm(
            IDurableOrchestrationContext context,
            SearchRequest searchRequest,
            MatchingAlgorithmResultSet searchResults,
            TimedResultSet<Dictionary<int, Donor>> donorInformation)
        {
            var matchPredictionInputs = await BuildMatchPredictionInputs(context, searchRequest, searchResults, donorInformation.ResultSet);
            var matchPredictionTasks = matchPredictionInputs.Select(r => RunMatchPredictionForDonorBatch(context, r)).ToList();
            var matchPredictionResults = (await Task.WhenAll(matchPredictionTasks)).SelectMany(x => x).ToDictionary();

            // We cannot use a stopwatch, as orchestration functions must be deterministic, and may be called multiple times.
            // Results of activity functions are constant across multiple invocations, so we can trust that finished time of the previous stage will remain constant 
            TimeSpan? totalElapsedTime = default;
            if (donorInformation.FinishedTimeUtc.HasValue)
            {
                var now = context.CurrentUtcDateTime;
                totalElapsedTime = now.Subtract(donorInformation.FinishedTimeUtc.Value);
            }

            context.SetCustomStatus(new OrchestrationStatus
            {
                LastCompletedStage = nameof(RunMatchPredictionAlgorithm),
                ElapsedTimeOfStage = totalElapsedTime,
            });

            return new TimedResultSet<Dictionary<int, MatchProbabilityResponse>>
            {
                ResultSet = matchPredictionResults,
                // If the previous stage did not successfully report a timespan, we do not want to report an error - so we allow it to be null.
                // TimedResultSet promises a non-null timestamp, so return MaxValue to be clear that this timing was not successful
                ElapsedTime = totalElapsedTime ?? TimeSpan.MaxValue
            };
        }

        private static async Task<IEnumerable<MultipleDonorMatchProbabilityInput>> BuildMatchPredictionInputs(
            IDurableOrchestrationContext context,
            SearchRequest searchRequest,
            MatchingAlgorithmResultSet searchResults,
            Dictionary<int, Donor> donorInformation)
        {
            return await context.CallActivityWithRetryAsync<IEnumerable<MultipleDonorMatchProbabilityInput>>(
                nameof(SearchActivityFunctions.BuildMatchPredictionInputs),
                RetryOptions,
                new MatchPredictionInputParameters
                {
                    SearchRequest = searchRequest,
                    MatchingAlgorithmResults = searchResults,
                    DonorDictionary = donorInformation
                });
        }

        private static async Task<TimedResultSet<Dictionary<int, Donor>>> FetchDonorInformation(
            IDurableOrchestrationContext context,
            MatchingAlgorithmResultSet matchingAlgorithmResults)
        {
            var activityInput = new Tuple<string, IEnumerable<int>>(
                context.InstanceId,
                matchingAlgorithmResults.MatchingAlgorithmResults.Select(r => r.AtlasDonorId)
            );

            var donorInfo = await context.CallActivityWithRetryAsync<TimedResultSet<Dictionary<int, Donor>>>(
                nameof(SearchActivityFunctions.FetchDonorInformation),
                RetryOptions,
                activityInput
            );

            context.SetCustomStatus(new OrchestrationStatus
            {
                LastCompletedStage = nameof(FetchDonorInformation),
                ElapsedTimeOfStage = null,
            });

            return donorInfo;
        }

        /// <returns>A Task returning a Key Value pair of Atlas Donor ID, and match prediction response.</returns>
        private static async Task<IReadOnlyDictionary<int, MatchProbabilityResponse>> RunMatchPredictionForDonorBatch(
            IDurableOrchestrationContext context,
            MultipleDonorMatchProbabilityInput matchProbabilityInput
        )
        {
            return await context.CallActivityWithRetryAsync<IReadOnlyDictionary<int, MatchProbabilityResponse>>(
                nameof(SearchActivityFunctions.RunMatchPrediction),
                RetryOptions,
                matchProbabilityInput
            );
        }

        private static async Task PersistSearchResults(
            IDurableOrchestrationContext context,
            TimedResultSet<MatchingAlgorithmResultSet> searchResults,
            TimedResultSet<Dictionary<int, MatchProbabilityResponse>> matchPredictionResults,
            Dictionary<int, Donor> donorInformation,
            DateTime searchInitiated)
        {
            // Note that this serializes the match prediction results, using default settings - which rounds any decimals to 15dp.
            await context.CallActivityWithRetryAsync(
                nameof(SearchActivityFunctions.PersistSearchResults),
                RetryOptions,
                new SearchActivityFunctions.PersistSearchResultsParameters
                {
                    DonorInformation = donorInformation,
                    MatchPredictionResults = matchPredictionResults,
                    MatchingAlgorithmResultSet = searchResults,
                    SearchInitiated = searchInitiated
                }
            );

            context.SetCustomStatus(new OrchestrationStatus
            {
                LastCompletedStage = nameof(PersistSearchResults),
                ElapsedTimeOfStage = null,
            });
        }
    }
}