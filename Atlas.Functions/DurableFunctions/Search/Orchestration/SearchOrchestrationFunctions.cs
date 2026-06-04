using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.LogFile;
using Atlas.Client.Models.Search.Results.Matching;
using MatchingAlgorithmFailureInfo = Atlas.Client.Models.Search.Results.Matching.MatchingAlgorithmFailureInfo;
using Atlas.Common.ApplicationInsights;
using Atlas.Functions.DurableFunctions.Search.Activity;
using Atlas.Functions.Exceptions;
using Atlas.Functions.Models;
using Atlas.Functions.Settings;
using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Common.Models;
using AutoMapper;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
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
    public class SearchOrchestrationFunctions
    {
        private static readonly TaskOptions RetryOptions =
            new(new TaskRetryOptions(new RetryPolicy(5, TimeSpan.FromSeconds(5), backoffCoefficient: 2)));

        private readonly IAtlasLogger logger;
        private readonly IMapper mapper;
        private readonly SearchLoggingContext loggingContext;
        private readonly int matchPredictionProcessingBatchSize;

        public SearchOrchestrationFunctions(ISearchLogger<SearchLoggingContext> logger,
            IMapper mapper,
            IOptions<AzureStorageSettings> azureStorageSettings,
            SearchLoggingContext loggingContext)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.loggingContext = loggingContext;
            matchPredictionProcessingBatchSize = azureStorageSettings.Value.MatchPredictionProcessingBatchSize;
        }

        [Function(nameof(SearchOrchestrator))]
        public async Task<SearchOrchestrationOutput> SearchOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
        {
            var parameters = context.GetInput<SearchOrchestratorParameters>();
            var notification = parameters.MatchingResultsNotification;
            var requestInfo = mapper.Map<SearchRequestIdentifiers>(notification);
            var orchestrationStartTime = context.CurrentUtcDateTime;

            loggingContext.SearchRequestId = requestInfo.SearchRequestId;
            var trackingSearchIdentifier = new Guid(requestInfo.SearchRequestId);
            var originalSearchIdentifier = requestInfo.RepeatSearchRequestId != null
                ? new Guid(requestInfo.RepeatSearchRequestId)
                : (Guid?)null;

            // Guard 1: matching algorithm failed — nothing further to orchestrate
            if (!notification.WasSuccessful)
            {
                await SendFailureNotification(context, requestInfo, "Matching Algorithm", notification.FailureInfo);
                return null;
            }

            // Guard 2: parallel path — dispatch to ACA Worker and exit; the aggregator handles
            // result persistence, log upload, and completion tracking from here on
            if (notification.SearchRequest?.ParallelMatchPrediction == true)
            {
                await SendMatchPredictionProcessInitiated(context, new MatchPredictionProcessInitiatedParameters { SearchIdentifier = trackingSearchIdentifier, OriginalSearchIdentifier = originalSearchIdentifier, InitiationTimeUtc = orchestrationStartTime, IsParallelMatchPrediction = true });
                await PrepareAndDispatchParallelMatchPredictionBatches(context, notification, requestInfo, orchestrationStartTime);
                return new SearchOrchestrationOutput { MatchingDonorCount = notification.NumberOfResults ?? -1 };
            }

            // Sequential path: run match prediction inline, then persist results and emit metrics
            var requestCompletedSuccessfully = false;
            TimedResultSet<IReadOnlyDictionary<int, string>> matchPredictionResultLocations = null;
            int? matchPredictionNumberOfBatches = null;
            MatchPredictionFailureInfo matchPredictionFailureInfo = null;

            try
            {
                await SendMatchPredictionProcessInitiated(context, new MatchPredictionProcessInitiatedParameters { SearchIdentifier = trackingSearchIdentifier, OriginalSearchIdentifier = originalSearchIdentifier, InitiationTimeUtc = orchestrationStartTime, IsParallelMatchPrediction = false });

                var matchPredictionRequestLocations = await PrepareMatchPrediction(context, notification, requestInfo);
                await SendMatchPredictionBatchProcessingStarted(context, new MatchPredictionSearchIdentifiers { SearchIdentifier = trackingSearchIdentifier, OriginalSearchIdentifier = originalSearchIdentifier });
                (matchPredictionResultLocations, matchPredictionNumberOfBatches)
                    = await RunMatchPredictionAlgorithm(context, requestInfo, matchPredictionRequestLocations);

                await SendMatchPredictionBatchProcessingEnded(context, new MatchPredictionSearchIdentifiers { SearchIdentifier = trackingSearchIdentifier, OriginalSearchIdentifier = originalSearchIdentifier });

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
            catch (HandledOrchestrationException e)
            {
                // Exceptions wrapped in HandledOrchestrationException have already been handled and failure notifications sent.
                // Re-throw so the function is tracked as a failure.
                matchPredictionFailureInfo = new MatchPredictionFailureInfo
                {
                    Type = MatchPredictionFailureType.OrchestrationError,
                    ExceptionStacktrace = e.ToString(),
                    Message = e.Message
                };

                throw;
            }
            catch (Exception e)
            {
                if (!context.IsReplaying)
                {
                    logger.SendException(e, LogLevel.Error, new Dictionary<string, string>
                    {
                        { "Stage", "Orchestrator" },
                        { "SearchRequestId", requestInfo.SearchRequestId }
                    });
                }

                // An unexpected exception occurred in the orchestration code itself — send a failure notification
                await SendFailureNotification(context, requestInfo, "Orchestrator");

                matchPredictionFailureInfo = new MatchPredictionFailureInfo
                {
                    Type = MatchPredictionFailureType.UnexpectedError,
                    ExceptionStacktrace = e.ToString(),
                    Message = e.Message
                };

                throw;
            }
            finally
            {
                var performanceMetrics = new RequestPerformanceMetrics
                {
                    InitiationTime = parameters.InitiationTime,
                    StartTime = orchestrationStartTime,
                    CompletionTime = context.CurrentUtcDateTime,
                    AlgorithmCoreStepDuration = matchPredictionResultLocations?.ElapsedTime,
                };

                await UploadSearchLogs(context, new SearchLog
                {
                    SearchRequestId = notification.SearchRequestId,
                    WasSuccessful = requestCompletedSuccessfully,
                    SearchRequest = notification.SearchRequest,
                    RequestPerformanceMetrics = performanceMetrics
                });

                await SendMatchPredictionProcessCompleted(context, new MatchPredictionProcessCompletedParameters
                {
                    SearchIdentifier = trackingSearchIdentifier,
                    OriginalSearchIdentifier = originalSearchIdentifier,
                    IsSuccessful = requestCompletedSuccessfully,
                    FailureInfo = matchPredictionFailureInfo,
                    DonorsPerBatch = matchPredictionProcessingBatchSize,
                    TotalNumberOfBatches = matchPredictionNumberOfBatches
                });
            }
        }

        [Function(nameof(RepeatSearchOrchestrator))]
        public async Task<SearchOrchestrationOutput> RepeatSearchOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
        {
            var notification = context.GetInput<MatchingResultsNotification>();
            var requestInfo = mapper.Map<SearchRequestIdentifiers>(notification);
            var orchestrationStartTime = context.CurrentUtcDateTime;

            loggingContext.SearchRequestId = requestInfo.SearchRequestId;
            var trackingSearchIdentifier = new Guid(requestInfo.RepeatSearchRequestId);
            var originalSearchIdentifier = new Guid(requestInfo.SearchRequestId);

            // Guard 1: matching algorithm failed — nothing further to orchestrate
            if (!notification.WasSuccessful)
            {
                await SendFailureNotification(context, requestInfo, "Matching Algorithm", notification.FailureInfo);
                return null;
            }

            // Guard 2: parallel path — dispatch to ACA Worker and exit; the aggregator handles
            // result persistence, log upload, and completion tracking from here on
            if (notification.SearchRequest?.ParallelMatchPrediction == true)
            {
                await SendMatchPredictionProcessInitiated(context, new MatchPredictionProcessInitiatedParameters { SearchIdentifier = trackingSearchIdentifier, OriginalSearchIdentifier = originalSearchIdentifier, InitiationTimeUtc = orchestrationStartTime, IsParallelMatchPrediction = true });
                await PrepareAndDispatchParallelMatchPredictionBatches(context, notification, requestInfo, orchestrationStartTime);
                return new SearchOrchestrationOutput { MatchingDonorCount = notification.NumberOfResults ?? -1 };
            }

            // Sequential path: run match prediction inline, then persist results and emit metrics
            int? matchPredictionNumberOfBatches = null;
            MatchPredictionFailureInfo matchPredictionFailureInfo = null;
            var requestCompletedSuccessfully = false;

            try
            {
                await SendMatchPredictionProcessInitiated(context, new MatchPredictionProcessInitiatedParameters { SearchIdentifier = trackingSearchIdentifier, OriginalSearchIdentifier = originalSearchIdentifier, InitiationTimeUtc = orchestrationStartTime, IsParallelMatchPrediction = false });

                var matchPredictionRequestLocations = await PrepareMatchPrediction(context, notification, requestInfo);
                await SendMatchPredictionBatchProcessingStarted(context, new MatchPredictionSearchIdentifiers { SearchIdentifier = trackingSearchIdentifier, OriginalSearchIdentifier = originalSearchIdentifier });
                var (matchPredictionResultLocations, numberOfBatches)
                    = await RunMatchPredictionAlgorithm(context, requestInfo, matchPredictionRequestLocations);
                matchPredictionNumberOfBatches = numberOfBatches;

                await SendMatchPredictionBatchProcessingEnded(context, new MatchPredictionSearchIdentifiers { SearchIdentifier = trackingSearchIdentifier, OriginalSearchIdentifier = originalSearchIdentifier });

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
            catch (HandledOrchestrationException e)
            {
                // Exceptions wrapped in HandledOrchestrationException have already been handled and failure notifications sent.
                // Re-throw so the function is tracked as a failure.
                matchPredictionFailureInfo = new MatchPredictionFailureInfo
                {
                    Type = MatchPredictionFailureType.OrchestrationError,
                    ExceptionStacktrace = e.ToString(),
                    Message = e.Message
                };

                throw;
            }
            catch (Exception e)
            {
                if (!context.IsReplaying)
                {
                    logger.SendException(e, LogLevel.Error, new Dictionary<string, string>
                    {
                        { "Stage", "Orchestrator" },
                        { "SearchRequestId", requestInfo.SearchRequestId }
                    });
                }

                // An unexpected exception occurred in the orchestration code itself — send a failure notification
                await SendFailureNotification(context, requestInfo, "Orchestrator");

                matchPredictionFailureInfo = new MatchPredictionFailureInfo
                {
                    Type = MatchPredictionFailureType.UnexpectedError,
                    ExceptionStacktrace = e.ToString(),
                    Message = e.Message
                };

                throw;
            }
            finally
            {
                await SendMatchPredictionProcessCompleted(context, new MatchPredictionProcessCompletedParameters
                {
                    SearchIdentifier = trackingSearchIdentifier,
                    OriginalSearchIdentifier = originalSearchIdentifier,
                    IsSuccessful = requestCompletedSuccessfully,
                    FailureInfo = matchPredictionFailureInfo,
                    DonorsPerBatch = matchPredictionProcessingBatchSize,
                    TotalNumberOfBatches = matchPredictionNumberOfBatches
                });
            }
        }

        private async Task PrepareAndDispatchParallelMatchPredictionBatches(
            TaskOrchestrationContext context,
            MatchingResultsNotification notification,
            SearchRequestIdentifiers requestInfo,
            DateTime orchestrationStartTime)
        {
            await RunStageAndHandleFailures(
                async () => await context.CallActivityAsync(
                    nameof(SearchActivityFunctions.PrepareAndDispatchParallelMatchPredictionBatches),
                    new PrepareAndDispatchParallelMatchPredictionBatchesParameters
                    {
                        MatchingResultsNotification = notification,
                        SearchInitiatedTimeUtc = orchestrationStartTime,
                    },
                    RetryOptions),
                context,
                requestInfo,
                nameof(SearchActivityFunctions.PrepareAndDispatchParallelMatchPredictionBatches)
            );

            context.SetCustomStatus(new OrchestrationStatus
            {
                LastCompletedStage = nameof(SearchActivityFunctions.PrepareAndDispatchParallelMatchPredictionBatches),
                ElapsedTimeOfStage = null,
            });
        }

        private async Task<TimedResultSet<IList<string>>> PrepareMatchPrediction(
            TaskOrchestrationContext context,
            MatchingResultsNotification notification,
            SearchRequestIdentifiers requestInfo)
        {
            var batchIds = await RunStageAndHandleFailures(async () =>
                    await context.CallActivityAsync<TimedResultSet<IList<string>>>(
                        nameof(SearchActivityFunctions.PrepareMatchPredictionBatches),
                        notification,
                        RetryOptions
                    ),
                context,
                requestInfo,
                nameof(SearchActivityFunctions.PrepareMatchPredictionBatches)
            );

            context.SetCustomStatus(new OrchestrationStatus
            {
                LastCompletedStage = "Prepare Match Prediction Batches",
                ElapsedTimeOfStage = notification.ElapsedTime,
            });

            return batchIds;
        }

        private async Task<(TimedResultSet<IReadOnlyDictionary<int, string>> TimedResultSets, int MatchPredictionNumberOfBatches)> RunMatchPredictionAlgorithm(
            TaskOrchestrationContext context,
            SearchRequestIdentifiers requestInfo,
            TimedResultSet<IList<string>> matchPredictionRequestLocations
        )
        {
            var matchPredictionTasksList =
                matchPredictionRequestLocations.ResultSet.Select(r => RunMatchPredictionForDonorBatch(context, r)).ToList();
            var matchPredictionResultLocations = new List<KeyValuePair<int, string>>();
            var matchPredictionTaskBatches = matchPredictionTasksList.Batch(matchPredictionProcessingBatchSize > 0
                ? matchPredictionProcessingBatchSize
                : matchPredictionRequestLocations.ResultSet.Count);
            var matchPredictionNumberOfBatches = matchPredictionTaskBatches.Count();

            foreach (var matchPredictionTasks in matchPredictionTasksList.Batch(matchPredictionProcessingBatchSize > 0
                         ? matchPredictionProcessingBatchSize
                         : matchPredictionRequestLocations.ResultSet.Count))
            {
                matchPredictionResultLocations.AddRange(
                    (await RunStageAndHandleFailures(
                        async () => await Task.WhenAll(matchPredictionTasks),
                        context,
                        requestInfo,
                        nameof(RunMatchPredictionAlgorithm)
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

            var timedResultSet = new TimedResultSet<IReadOnlyDictionary<int, string>>
            {
                ResultSet = ToDictionaryExtension.ToDictionary(matchPredictionResultLocations),
                ElapsedTime = totalElapsedTime ?? TimeSpan.MaxValue
            };
            return (timedResultSet, matchPredictionNumberOfBatches);
        }

        /// <returns>A Task a list of locations in which MPA results (per donor) can be found.</returns>
        private static async Task<IReadOnlyDictionary<int, string>> RunMatchPredictionForDonorBatch(
            TaskOrchestrationContext context,
            string requestLocation
        )
        {
            // Do not add error handling to this, as we will then see multiple failure notifications with multiple batches
            return await context.CallActivityAsync<IReadOnlyDictionary<int, string>>(
                nameof(SearchActivityFunctions.RunMatchPredictionBatch),
                requestLocation,
                RetryOptions
            );
        }

        private async Task PersistSearchResults(
            TaskOrchestrationContext context,
            PersistSearchResultsFunctionParameters parameters,
            SearchRequestIdentifiers requestInfo
        )
        {
            await RunStageAndHandleFailures(
                async () => await context.CallActivityAsync(nameof(SearchActivityFunctions.PersistSearchResults), parameters,
                    RetryOptions),
                context,
                requestInfo,
                nameof(PersistSearchResults)
            );

            context.SetCustomStatus(new OrchestrationStatus
            {
                LastCompletedStage = nameof(PersistSearchResults),
                ElapsedTimeOfStage = null,
            });
        }

        private async Task RunStageAndHandleFailures(
            Func<Task> runStage,
            TaskOrchestrationContext context,
            SearchRequestIdentifiers requestInfo,
            string stageReached) =>
            await RunStageAndHandleFailures(async () =>
            {
                await runStage();
                return true;
            }, context, requestInfo, stageReached);

        private async Task<T> RunStageAndHandleFailures<T>(
            Func<Task<T>> runStage,
            TaskOrchestrationContext context,
            SearchRequestIdentifiers requestInfo,
            string stageReached)
        {
            try
            {
                return await runStage();
            }
            catch (Exception e)
            {
                if (!context.IsReplaying)
                {
                    logger.SendException(e, LogLevel.Error, new Dictionary<string, string>
                    {
                        { "Stage", stageReached },
                        { "SearchRequestId", requestInfo.SearchRequestId }
                    });
                }
                await SendFailureNotification(context, requestInfo, stageReached);
                throw new HandledOrchestrationException(e);
            }
        }

        private static async Task SendFailureNotification(
            TaskOrchestrationContext context,
            SearchRequestIdentifiers requestInfo,
            string stageReached,
            MatchingAlgorithmFailureInfo matchingAlgorithmFailureInfo = null)
        {
            await context.CallActivityAsync(
                nameof(SearchActivityFunctions.SendFailureNotification),
                new SendFailureNotificationParameters
                {
                    SearchRequestId = requestInfo.SearchRequestId,
                    RepeatSearchRequestId = requestInfo.RepeatSearchRequestId,
                    StageReached = stageReached,
                    MatchingAlgorithmFailureInfo = matchingAlgorithmFailureInfo
                },
                RetryOptions
            );

            context.SetCustomStatus($"Search failed, during stage: {stageReached}");
        }

        private static async Task UploadSearchLogs(
            TaskOrchestrationContext context,
            SearchLog searchLog) =>
            await context.CallActivityAsync(
                nameof(SearchActivityFunctions.UploadSearchLog),
                searchLog,
                RetryOptions
            );

        private static async Task SendMatchPredictionProcessInitiated(
            TaskOrchestrationContext context,
            MatchPredictionProcessInitiatedParameters parameters) =>
            await context.CallActivityAsync(
                nameof(SearchActivityFunctions.SendMatchPredictionProcessInitiated),
                parameters,
                RetryOptions
            );

        private static async Task SendMatchPredictionBatchProcessingStarted(
            TaskOrchestrationContext context,
            MatchPredictionSearchIdentifiers parameters) =>
            await context.CallActivityAsync(
                nameof(SearchActivityFunctions.SendMatchPredictionBatchProcessingStarted),
                parameters,
                RetryOptions
            );

        private static async Task SendMatchPredictionBatchProcessingEnded(
            TaskOrchestrationContext context,
            MatchPredictionSearchIdentifiers parameters) =>
            await context.CallActivityAsync(
                nameof(SearchActivityFunctions.SendMatchPredictionBatchProcessingEnded),
                parameters,
                RetryOptions
            );

        private static async Task SendMatchPredictionProcessCompleted(
            TaskOrchestrationContext context,
            MatchPredictionProcessCompletedParameters parameters) =>
            await context.CallActivityAsync(
                nameof(SearchActivityFunctions.SendMatchPredictionProcessCompleted),
                parameters,
                RetryOptions
            );
    }
}