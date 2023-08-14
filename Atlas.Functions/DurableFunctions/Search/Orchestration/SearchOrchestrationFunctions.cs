using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.LogFile;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Common.ApplicationInsights;
using Atlas.Functions.DurableFunctions.Search.Activity;
using Atlas.Functions.Exceptions;
using Atlas.Functions.Models;
using Atlas.Functions.Settings;
using AutoMapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Options;
using MoreLinq.Extensions;

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
        private static readonly RetryOptions RetryOptions = new(TimeSpan.FromSeconds(5), 5) { BackoffCoefficient = 2 };

        private readonly ILogger logger;
        private readonly IMapper mapper;
        private readonly SearchLoggingContext loggingContext;
        private readonly int matchPredictionProcessingBatchSize;

        public SearchOrchestrationFunctions(ISearchLogger<SearchLoggingContext> logger, IMapper mapper, IOptions<AzureStorageSettings> azureStorageSettings, SearchLoggingContext loggingContext)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.loggingContext = loggingContext;
            matchPredictionProcessingBatchSize = azureStorageSettings.Value.MatchPredictionProcessingBatchSize;
        }

        [FunctionName(nameof(SearchOrchestrator))]
        public async Task<SearchOrchestrationOutput> SearchOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var parameters = context.GetInput<SearchOrchestratorParameters>();
            var notification = parameters.MatchingResultsNotification;
            var requestInfo = mapper.Map<FailureNotificationRequestInfo>(notification);
            var orchestrationStartTime = context.CurrentUtcDateTime;
            var requestCompletedSuccessfully = false;
            TimedResultSet<IReadOnlyDictionary<int, string>> matchPredictionResultLocations = null;

            loggingContext.SearchRequestId = requestInfo.SearchRequestId;

            try
            {
                if (!notification.WasSuccessful)
                {
                    requestInfo.StageReached = "Matching Algorithm";
                    await SendFailureNotification(context, requestInfo);
                    // returning early to prevent unnecessary retries of the search request
                    return null;
                }

                var matchPredictionRequestLocations = await PrepareMatchPrediction(context, notification, requestInfo);
                matchPredictionResultLocations = await RunMatchPredictionAlgorithm(context, requestInfo, matchPredictionRequestLocations);
                await PersistSearchResults(
                    context,
                    new PersistSearchResultsFunctionParameters
                    {
                        SearchInitiated = orchestrationStartTime,
                        MatchingResultsNotification = notification,
                        MatchPredictionResultLocations = matchPredictionResultLocations
                    },
                    requestInfo);

                requestCompletedSuccessfully = true;

                // "return" populates the "output" property on the status check GET endpoint set up by the durable functions framework
                return new SearchOrchestrationOutput
                {
                    TotalSearchTime = context.CurrentUtcDateTime.Subtract(orchestrationStartTime),
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
                requestInfo.StageReached = "Orchestrator";
                await SendFailureNotification(context, requestInfo);
                throw;
            }
            finally
            {
                var performanceMetrics = new RequestPerformanceMetrics
                {
                    InitiationTime = parameters.InitiationTime,
                    StartTime = orchestrationStartTime,
                    CompletionTime = context.CurrentUtcDateTime,
                    AlgorithmCoreStepDuration = matchPredictionResultLocations?.ElapsedTime
                };

                await UploadSearchLogs(context, new SearchLog
                {
                    SearchRequestId = notification.SearchRequestId,
                    WasSuccessful = requestCompletedSuccessfully,
                    SearchRequest = notification.SearchRequest,
                    RequestPerformanceMetrics = performanceMetrics
                });
            }
        }

        [FunctionName(nameof(RepeatSearchOrchestrator))]
        public async Task<SearchOrchestrationOutput> RepeatSearchOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var notification = context.GetInput<MatchingResultsNotification>();
            var requestInfo = mapper.Map<FailureNotificationRequestInfo>(notification);

            try
            {
                var orchestrationInitiated = context.CurrentUtcDateTime;
                if (!notification.WasSuccessful)
                {
                    requestInfo.StageReached = "Matching Algorithm";
                    await SendFailureNotification(context, requestInfo);
                    // returning early to avoid unnecessary retries of the search request
                    return null;
                }

                var matchPredictionRequestLocations = await PrepareMatchPrediction(context, notification, requestInfo);
                var matchPredictionResultLocations = await RunMatchPredictionAlgorithm(context, requestInfo, matchPredictionRequestLocations);
                await PersistSearchResults(
                    context,
                    new PersistSearchResultsFunctionParameters
                    {
                        SearchInitiated = orchestrationInitiated,
                        MatchingResultsNotification = notification,
                        MatchPredictionResultLocations = matchPredictionResultLocations
                    },
                    requestInfo);

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
                requestInfo.StageReached = "Orchestrator";
                await SendFailureNotification(context, requestInfo);
                throw;
            }
        }

        private async Task<TimedResultSet<IList<string>>> PrepareMatchPrediction(
            IDurableOrchestrationContext context,
            MatchingResultsNotification notification,
            FailureNotificationRequestInfo requestInfo)
        {
            requestInfo.StageReached = nameof(SearchActivityFunctions.PrepareMatchPredictionBatches);

            var batchIds = await RunStageAndHandleFailures(async () =>
                    await context.CallActivityWithRetryAsync<TimedResultSet<IList<string>>>(
                        nameof(SearchActivityFunctions.PrepareMatchPredictionBatches),
                        RetryOptions,
                        notification
                    ),
                context,
                requestInfo
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
            FailureNotificationRequestInfo requestInfo,
            TimedResultSet<IList<string>> matchPredictionRequestLocations
        )
        {
            requestInfo.StageReached = nameof(RunMatchPredictionAlgorithm);

            var matchPredictionTasksList = matchPredictionRequestLocations.ResultSet.Select(r => RunMatchPredictionForDonorBatch(context, r)).ToList();
            var matchPredictionResultLocations = new List<KeyValuePair<int, string>>();

            foreach (var matchPredictionTasks in matchPredictionTasksList.Batch(matchPredictionProcessingBatchSize > 0 ? matchPredictionProcessingBatchSize : matchPredictionRequestLocations.ResultSet.Count))
            {
                matchPredictionResultLocations.AddRange(
                    (await RunStageAndHandleFailures(
                        async () => await Task.WhenAll(matchPredictionTasks),
                        context,
                        requestInfo
                    ))
                    .SelectMany(x => x));
            }

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
                ResultSet = matchPredictionResultLocations.ToDictionary(),
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
            PersistSearchResultsFunctionParameters parameters,
            FailureNotificationRequestInfo requestInfo
        )
        {
            requestInfo.StageReached = nameof(PersistSearchResults);

            await RunStageAndHandleFailures(
                async () => await context.CallActivityWithRetryAsync(nameof(SearchActivityFunctions.PersistSearchResults), RetryOptions,
                    parameters),
                context,
                requestInfo
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
            FailureNotificationRequestInfo requestInfo) =>
            await RunStageAndHandleFailures(async () =>
            {
                await runStage();
                return true;
            }, context, requestInfo);

        private async Task<T> RunStageAndHandleFailures<T>(
            Func<Task<T>> runStage,
            IDurableOrchestrationContext context,
            FailureNotificationRequestInfo requestInfo)
        {
            try
            {
                return await runStage();
            }
            catch (Exception e)
            {
                logger.SendTrace($"Failure at stage: {requestInfo.StageReached}. Exception: {e.Message}, {e.InnerException?.Message}");
                await SendFailureNotification(context, requestInfo);
                throw new HandledOrchestrationException(e);
            }
        }

        private static async Task SendFailureNotification(
            IDurableOrchestrationContext context,
            FailureNotificationRequestInfo requestInfo)
        {
            await context.CallActivityWithRetryAsync(
                nameof(SearchActivityFunctions.SendFailureNotification),
                RetryOptions,
                requestInfo
            );

            context.SetCustomStatus($"Search failed, during stage: {requestInfo.StageReached}");
        }

        private static async Task UploadSearchLogs(
            IDurableOrchestrationContext context,
            SearchLog searchLog) =>
            await context.CallActivityWithRetryAsync(
                nameof(SearchActivityFunctions.UploadSearchLog),
                RetryOptions,
                searchLog
            );
    }
}