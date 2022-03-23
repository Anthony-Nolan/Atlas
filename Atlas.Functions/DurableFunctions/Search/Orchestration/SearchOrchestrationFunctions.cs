using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.ApplicationInsights;
using Atlas.Functions.DurableFunctions.Search.Activity;
using Atlas.Functions.Exceptions;
using Atlas.Functions.Models;
using Atlas.Functions.Services.BlobStorageClients;
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
        private static readonly RetryOptions RetryOptions = new RetryOptions(TimeSpan.FromSeconds(5), 5) { BackoffCoefficient = 2 };

        private readonly ILogger logger;

        public SearchOrchestrationFunctions(ILogger logger)
        {
            this.logger = logger;
        }

        [FunctionName(nameof(SearchOrchestrator))]
        public async Task<SearchOrchestrationOutput> SearchOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var notification = context.GetInput<MatchingResultsNotification>();
            var searchId = notification.SearchRequestId;
            var repeatSearchId = notification.RepeatSearchRequestId;

            try
            {
                var orchestrationInitiated = context.CurrentUtcDateTime;
                if (!notification.WasSuccessful)
                {
                    await SendFailureNotification(context, "Matching Algorithm", searchId, repeatSearchId);
                    return null;
                }

                var matchPredictionRequestLocations = await PrepareMatchPrediction(context, notification);
                var matchPredictionResultLocations =
                    await RunMatchPredictionAlgorithm(context, searchId, repeatSearchId, matchPredictionRequestLocations);
                await PersistSearchResults(context, new SearchActivityFunctions.PersistSearchResultsParameters
                {
                    SearchInitiated = orchestrationInitiated,
                    MatchingResultsNotification = notification,
                    MatchPredictionResultLocations = matchPredictionResultLocations
                });

                // "return" populates the "output" property on the status check GET endpoint set up by the durable functions framework
                return new SearchOrchestrationOutput
                {
                    TotalSearchTime = context.CurrentUtcDateTime.Subtract(orchestrationInitiated),
                    MatchingDonorCount = notification.NumberOfResults ?? -1,
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
                await SendFailureNotification(context, "Orchestrator", searchId, repeatSearchId);
                throw;
            }
        }

        [FunctionName(nameof(RepeatSearchOrchestrator))]
        public async Task<SearchOrchestrationOutput> RepeatSearchOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var notification = context.GetInput<MatchingResultsNotification>();
            var searchRequestId = notification.SearchRequestId;

            try
            {
                var orchestrationInitiated = context.CurrentUtcDateTime;
                if (!notification.WasSuccessful)
                {
                    await SendFailureNotification(
                        context,
                        "Matching Algorithm",
                        searchRequestId,
                        notification.RepeatSearchRequestId,
                        notification.ValidationError
                    );
                    return null;
                }

                var matchPredictionRequestLocations = await PrepareMatchPrediction(context, notification);
                var matchPredictionResultLocations = await RunMatchPredictionAlgorithm(
                    context,
                    searchRequestId,
                    notification.RepeatSearchRequestId,
                    matchPredictionRequestLocations);
                await PersistSearchResults(context, new SearchActivityFunctions.PersistSearchResultsParameters
                {
                    SearchInitiated = orchestrationInitiated,
                    MatchingResultsNotification = notification,
                    MatchPredictionResultLocations = matchPredictionResultLocations
                });

                // "return" populates the "output" property on the status check GET endpoint set up by the durable functions framework
                return new SearchOrchestrationOutput
                {
                    TotalSearchTime = context.CurrentUtcDateTime.Subtract(orchestrationInitiated),
                    MatchingDonorCount = notification.NumberOfResults ?? -1,
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
                await SendFailureNotification(context, "Orchestrator", searchRequestId, notification.RepeatSearchRequestId);
                throw;
            }
        }

        private async Task<TimedResultSet<IList<string>>> PrepareMatchPrediction(
            IDurableOrchestrationContext context,
            MatchingResultsNotification notification)
        {
            var batchIds = await RunStageAndHandleFailures(async () =>
                    await context.CallActivityWithRetryAsync<TimedResultSet<IList<string>>>(
                        nameof(SearchActivityFunctions.PrepareMatchPredictionBatches),
                        RetryOptions,
                        notification
                    ),
                context,
                nameof(SearchActivityFunctions.PrepareMatchPredictionBatches),
                notification.SearchRequestId,
                notification.RepeatSearchRequestId
            );

            context.SetCustomStatus(new OrchestrationStatus
            {
                LastCompletedStage = "Prepare Match Prediction Batches",
                ElapsedTimeOfStage = notification.ElapsedTime,
            });

            return batchIds;
        }

        private async Task<TimedResultSet<IReadOnlyDictionary<int, string>>> RunMatchPredictionAlgorithm(
            IDurableOrchestrationContext context,
            string searchId,
            string repeatSearchId,
            TimedResultSet<IList<string>> matchPredictionRequestLocations
        )
        {
            var matchPredictionTasks = matchPredictionRequestLocations.ResultSet.Select(r => RunMatchPredictionForDonorBatch(context, r)).ToList();
            var matchPredictionResultLocations = (await RunStageAndHandleFailures(
                    async () => await Task.WhenAll(matchPredictionTasks),
                    context,
                    nameof(RunMatchPredictionAlgorithm),
                    searchId,
                    repeatSearchId
                ))
                .SelectMany(x => x)
                .ToDictionary();


            // We cannot use a stopwatch, as orchestration functions must be deterministic, and may be called multiple times.
            // Results of activity functions are constant across multiple invocations, so we can trust that finished time of the previous stage will remain constant 
            TimeSpan? totalElapsedTime = default;
            if (matchPredictionRequestLocations.FinishedTimeUtc.HasValue)
            {
                var now = context.CurrentUtcDateTime;
                totalElapsedTime = now.Subtract(matchPredictionRequestLocations.FinishedTimeUtc.Value);
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

        /// <returns>A Task a list of locations in which MPA results (per donor) can be found.</returns>
        private static async Task<IReadOnlyDictionary<int, string>> RunMatchPredictionForDonorBatch(
            IDurableOrchestrationContext context,
            string requestLocation
        )
        {
            // Do not add error handling to this, as we will then see multiple failure notifications with multiple batches
            return await context.CallActivityWithRetryAsync<IReadOnlyDictionary<int, string>>(
                nameof(SearchActivityFunctions.RunMatchPredictionBatch),
                RetryOptions,
                requestLocation
            );
        }

        private async Task PersistSearchResults(
            IDurableOrchestrationContext context,
            SearchActivityFunctions.PersistSearchResultsParameters persistSearchResultsParameters
        )
        {
            await RunStageAndHandleFailures(
                async () => await context.CallActivityWithRetryAsync(nameof(SearchActivityFunctions.PersistSearchResults), RetryOptions,
                    persistSearchResultsParameters),
                context,
                nameof(PersistSearchResults),
                persistSearchResultsParameters.MatchingResultsNotification.SearchRequestId,
                persistSearchResultsParameters.MatchingResultsNotification.RepeatSearchRequestId
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
            string searchId,
            string repeatSearchId) =>
            await RunStageAndHandleFailures(async () =>
            {
                await runStage();
                return true;
            }, context, stageName, searchId, repeatSearchId);

        private async Task<T> RunStageAndHandleFailures<T>(
            Func<Task<T>> runStage,
            IDurableOrchestrationContext context,
            string stageName,
            string searchId,
            string repeatSearchId)
        {
            try
            {
                return await runStage();
            }
            catch (Exception e)
            {
                logger.SendTrace($"Failure at stage: {stageName}. Exception: {e.Message}, {e.InnerException?.Message}");
                await SendFailureNotification(context, stageName, searchId, repeatSearchId);
                throw new HandledOrchestrationException(e);
            }
        }

        private static async Task SendFailureNotification(
            IDurableOrchestrationContext context,
            string failedStage,
            string searchId,
            string repeatSearchId,
            string validationError = null)
        {
            await context.CallActivityWithRetryAsync(
                nameof(SearchActivityFunctions.SendFailureNotification),
                RetryOptions,
                (searchId, failedStage, repeatSearchId, validationError)
            );

            context.SetCustomStatus($"Search failed, during stage: {failedStage}");
        }
    }
}