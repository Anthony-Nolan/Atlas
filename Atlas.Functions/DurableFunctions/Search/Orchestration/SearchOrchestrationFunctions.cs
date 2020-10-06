using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.ApplicationInsights;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.Functions.DurableFunctions.Search.Activity;
using Atlas.Functions.Exceptions;
using Atlas.Functions.Models;
using Atlas.Functions.Services;
using Atlas.Functions.Services.BlobStorageClients;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using MoreLinq.Extensions;
using Polly;

namespace Atlas.Functions.DurableFunctions.Search.Orchestration
{
    /// <summary>
    /// Note that orchestration triggered functions will be run multiple times per-request scope, so need to follow the code constraints
    /// as documented by Microsoft https://docs.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-code-constraints.
    /// Logging should be avoided in this function due to this.
    /// Any expensive or non-deterministic code should be called from an Activity function.
    /// </summary>
    // ReSharper disable once MemberCanBeInternal
    // ReSharper disable once ClassNeverInstantiated.Global
    public class SearchOrchestrationFunctions
    {
        private static readonly RetryOptions RetryOptions = new RetryOptions(TimeSpan.FromSeconds(5), 5) {BackoffCoefficient = 2};

        private readonly IResultsUploader searchResultsBlobUploader;
        private readonly IResultsCombiner resultsCombiner;
        private readonly ISearchCompletionMessageSender searchCompletionMessageSender;
        private readonly ILogger logger;

        public SearchOrchestrationFunctions(
            IResultsCombiner resultsCombiner,
            IResultsUploader searchResultsBlobUploader,
            ISearchCompletionMessageSender searchCompletionMessageSender, 
            ILogger logger)
        {
            this.resultsCombiner = resultsCombiner;
            this.searchResultsBlobUploader = searchResultsBlobUploader;
            this.searchCompletionMessageSender = searchCompletionMessageSender;
            this.logger = logger;
        }

        [FunctionName(nameof(SearchOrchestrator))]
        public async Task<SearchOrchestrationOutput> SearchOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var notification = context.GetInput<MatchingResultsNotification>();
            var searchId = notification.SearchRequestId;

            try
            {
                logger.SendTrace($"Search request {searchId} has orchestration instance id {context.InstanceId}", LogLevel.Verbose);
                var orchestrationInitiated = context.CurrentUtcDateTime;
                var searchRequest = notification.SearchRequest;
                if (!notification.WasSuccessful)
                {
                    await SendFailureNotification(context, "Matching Algorithm", searchId);
                    return null;
                }

                var timedSearchResults = await DownloadMatchingAlgorithmResults(context, notification);
                var searchResults = timedSearchResults.ResultSet;
                var timedDonorInformation = await FetchDonorInformation(context, searchResults, searchId);
                var donorInformation = timedDonorInformation.ResultSet;
                var matchPredictionResults =
                    await RunMatchPredictionAlgorithm(context, searchRequest, searchResults, timedDonorInformation, searchId);
                await PersistSearchResults(context, timedSearchResults, matchPredictionResults, donorInformation, orchestrationInitiated, searchId);
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
            catch (HandledOrchestrationException)
            {
                // Exceptions wrapper in "HandleOrchestrationException" have already been handled, and failure notifications sent.
                // In this case we should just re-throw so the function is tracked as a failure.
                throw;
            }
            catch (Exception e)
            {
                logger.SendTrace($"Failure during orchestration. Exception: {e.Message}, {e.InnerException?.Message}");
                // An unexpected exception occurred in the *orchestration* code. Ensure we send a failure notification
                await SendFailureNotification(context, "Orchestrator", searchId);
                throw;
            }
        }

        private async Task<TimedResultSet<MatchingAlgorithmResultSet>> DownloadMatchingAlgorithmResults(
            IDurableOrchestrationContext context,
            MatchingResultsNotification notification)
        {
            var matchingResults = await RunStageAndHandleFailures(async () =>
                    await context.CallActivityWithRetryAsync<TimedResultSet<MatchingAlgorithmResultSet>>(
                        nameof(SearchActivityFunctions.DownloadMatchingAlgorithmResults),
                        RetryOptions,
                        notification
                    ),
                context,
                nameof(DownloadMatchingAlgorithmResults),
                notification.SearchRequestId
            );

            context.SetCustomStatus(new OrchestrationStatus
            {
                LastCompletedStage = "MatchingAlgorithm",
                ElapsedTimeOfStage = notification.ElapsedTime,
            });

            return matchingResults;
        }

        private async Task<TimedResultSet<IReadOnlyDictionary<int, string>>> RunMatchPredictionAlgorithm(
            IDurableOrchestrationContext context,
            SearchRequest searchRequest,
            MatchingAlgorithmResultSet searchResults,
            TimedResultSet<Dictionary<int, Donor>> donorInformation,
            string searchId)
        {
            var matchPredictionInputs = await BuildMatchPredictionInputs(context, searchRequest, searchResults, donorInformation.ResultSet, searchId);
            var matchPredictionTasks = matchPredictionInputs.Select(r => RunMatchPredictionForDonorBatch(context, r)).ToList();
            var matchPredictionResultLocations = (await RunStageAndHandleFailures(
                    async () => await Task.WhenAll(matchPredictionTasks),
                    context,
                    nameof(RunMatchPredictionAlgorithm),
                    searchId
                ))
                .SelectMany(x => x)
                .ToDictionary();

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

            return new TimedResultSet<IReadOnlyDictionary<int, string>>
            {
                ResultSet = matchPredictionResultLocations,
                // If the previous stage did not successfully report a timespan, we do not want to report an error - so we allow it to be null.
                // TimedResultSet promises a non-null timestamp, so return MaxValue to be clear that this timing was not successful
                ElapsedTime = totalElapsedTime ?? TimeSpan.MaxValue
            };
        }

        private async Task<IEnumerable<MultipleDonorMatchProbabilityInput>> BuildMatchPredictionInputs(
            IDurableOrchestrationContext context,
            SearchRequest searchRequest,
            MatchingAlgorithmResultSet searchResults,
            Dictionary<int, Donor> donorInformation,
            string searchId
        )
        {
            return await RunStageAndHandleFailures(async () =>
                    await context.CallActivityWithRetryAsync<IEnumerable<MultipleDonorMatchProbabilityInput>>(
                        nameof(SearchActivityFunctions.BuildMatchPredictionInputs),
                        RetryOptions,
                        new MatchPredictionInputParameters
                        {
                            SearchRequest = searchRequest,
                            MatchingAlgorithmResults = searchResults,
                            DonorDictionary = donorInformation
                        }),
                context,
                nameof(BuildMatchPredictionInputs),
                searchId
            );
        }

        private async Task<TimedResultSet<Dictionary<int, Donor>>> FetchDonorInformation(
            IDurableOrchestrationContext context,
            MatchingAlgorithmResultSet matchingAlgorithmResults,
            string searchId)
        {
            var donorInfo = await RunStageAndHandleFailures(async () =>
                    await context.CallActivityWithRetryAsync<TimedResultSet<Dictionary<int, Donor>>>(
                        nameof(SearchActivityFunctions.FetchDonorInformation),
                        RetryOptions,
                        matchingAlgorithmResults.MatchingAlgorithmResults.Select(r => r.AtlasDonorId)
                    ),
                context,
                nameof(FetchDonorInformation),
                searchId
            );

            context.SetCustomStatus(new OrchestrationStatus
            {
                LastCompletedStage = nameof(FetchDonorInformation),
                ElapsedTimeOfStage = null,
            });

            return donorInfo;
        }

        /// <returns>A Task a list of locations in which MPA results (per donor) can be found.</returns>
        private static async Task<IReadOnlyDictionary<int, string>> RunMatchPredictionForDonorBatch(
            IDurableOrchestrationContext context,
            MultipleDonorMatchProbabilityInput matchProbabilityInput
        )
        {
            // Do not add error handling to this, as we will then see multiple failure notifications with multiple batches
            return await context.CallActivityWithRetryAsync<IReadOnlyDictionary<int, string>>(
                nameof(SearchActivityFunctions.RunMatchPrediction),
                RetryOptions,
                matchProbabilityInput
            );
        }

        private async Task PersistSearchResults(
            IDurableOrchestrationContext context,
            TimedResultSet<MatchingAlgorithmResultSet> searchResults,
            TimedResultSet<IReadOnlyDictionary<int, string>> matchPredictionResultLocations,
            Dictionary<int, Donor> donorInformation,
            DateTime searchInitiated,
            string searchId)
        {
            await RunStageAndHandleFailures(async () =>
                {
                    // The general recommendation is durable functions is to perform most actions via activity functions
                    // In this case we have found that uploading results via an activity causes a very large message payload to be created,
                    // zipped, stored in durable functions blob storage - with a delay of up to 10 minutes for large searches. 
                    // As we only want to upload to blob storage ourselves, we can shave off that delay by uploading directly from the orchestrator.
                    //
                    // The core downsides of this approach are: 
                    //    - We do not get the activity function retry handling, and instead need to handle this ourselves
                    //    - There is a risk of the upload process being triggered twice 
                    await Policy
                        .Handle<Exception>()
                        .RetryAsync(5)
                        .ExecuteAsync(async () =>
                        {
                            var parameters = new SearchActivityFunctions.PersistSearchResultsParameters
                            {
                                DonorInformation = donorInformation,
                                MatchPredictionResultLocations = matchPredictionResultLocations,
                                MatchingAlgorithmResultSet = searchResults,
                                SearchInitiated = searchInitiated
                            };

                            var resultSet = await resultsCombiner.CombineResults(parameters);
                            await searchResultsBlobUploader.UploadResults(resultSet);
                            await searchCompletionMessageSender.PublishResultsMessage(resultSet, parameters.SearchInitiated);
                        });
                },
                context,
                nameof(PersistSearchResults),
                searchId
            );

            context.SetCustomStatus(new OrchestrationStatus
            {
                LastCompletedStage = nameof(PersistSearchResults),
                ElapsedTimeOfStage = null,
            });
        }

        private async Task RunStageAndHandleFailures(
            Func<Task> runStage,
            IDurableOrchestrationContext context,
            string stageName,
            string searchId) =>
            await RunStageAndHandleFailures(async () =>
            {
                await runStage();
                return true;
            }, context, stageName, searchId);

        private async Task<T> RunStageAndHandleFailures<T>(
            Func<Task<T>> runStage,
            IDurableOrchestrationContext context,
            string stageName,
            string searchId)
        {
            try
            {
                return await runStage();
            }
            catch (Exception e)
            {
                logger.SendTrace($"Failure at stage: {stageName}. Exception: {e.Message}, {e.InnerException?.Message}");
                await SendFailureNotification(context, stageName, searchId);
                throw new HandledOrchestrationException(e);
            }
        }

        private static async Task SendFailureNotification(IDurableOrchestrationContext context, string failedStage, string searchId)
        {
            await context.CallActivityWithRetryAsync(
                nameof(SearchActivityFunctions.SendFailureNotification),
                RetryOptions,
                (searchId, failedStage)
            );

            context.SetCustomStatus($"Search failed, during stage: {failedStage}");
        }
    }
}