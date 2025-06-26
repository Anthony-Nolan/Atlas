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
    // ReSharper disable once MemberCanBeInternal
    // ReSharper disable once ClassNeverInstantiated.Global
    public class SearchOrchestrationFunctions
    {
        private static readonly TaskOptions RetryOptions =
            new(new TaskRetryOptions(new RetryPolicy(5, TimeSpan.FromSeconds(5), backoffCoefficient: 2)));

        private readonly ILogger logger;
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
            var requestInfo = mapper.Map<FailureNotificationRequestInfo>(notification);
            var orchestrationStartTime = context.CurrentUtcDateTime;
            var requestCompletedSuccessfully = false;
            TimedResultSet<IReadOnlyDictionary<int, string>> matchPredictionResultLocations = null;
            int? matchPredictionNumberOfBatches = null;
            MatchPredictionFailureInfo matchPredictionFailureInfo = null;

            loggingContext.SearchRequestId = requestInfo.SearchRequestId;
            var trackingSearchIdentifier = new Guid(requestInfo.SearchRequestId);
            var originalSearchIdentifier = requestInfo.RepeatSearchRequestId != null
                ? new Guid(requestInfo.RepeatSearchRequestId)
                : (Guid?)null;
            try
            {
                if (!notification.WasSuccessful)
                {
                    requestInfo.StageReached = "Matching Algorithm";
                    await SendFailureNotification(context, requestInfo);
                    // returning early to prevent unnecessary retries of the search request
                    return null;
                }

                await SendMatchPredictionProcessInitiated(context, (SearchIdentifier: trackingSearchIdentifier, OriginalSearchIdentifier: originalSearchIdentifier, InitiationTimeUtc: orchestrationStartTime));

                var matchPredictionRequestLocations = await PrepareMatchPrediction(context, notification, requestInfo);
                await SendMatchPredictionBatchProcessingStarted(context, (SearchIdentifier: trackingSearchIdentifier, OriginalSearchIdentifier: originalSearchIdentifier));
                (matchPredictionResultLocations, matchPredictionNumberOfBatches)
                    = await RunMatchPredictionAlgorithm(context, requestInfo, matchPredictionRequestLocations);

                await SendMatchPredictionBatchProcessingEnded(context, (SearchIdentifier: trackingSearchIdentifier, OriginalSearchIdentifier: originalSearchIdentifier));

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
                // Exceptions wrapper in "HandleOrchestrationException" have already been handled, and failure notifications sent.
                // In this case we should just re-throw so the function is tracked as a failure.

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
                logger.SendTrace($"Failure during orchestration. Exception: {e.Message}, {e.InnerException?.Message}");

                // An unexpected exception occurred in the *orchestration* code. Ensure we send a failure notification
                requestInfo.StageReached = "Orchestrator";
                await SendFailureNotification(context, requestInfo);

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
                    AlgorithmCoreStepDuration = matchPredictionResultLocations.ElapsedTime,
                };

                await UploadSearchLogs(context, new SearchLog
                {
                    SearchRequestId = notification.SearchRequestId,
                    WasSuccessful = requestCompletedSuccessfully,
                    SearchRequest = notification.SearchRequest,
                    RequestPerformanceMetrics = performanceMetrics
                });

                if (notification.WasSuccessful)
                {
                    await SendMatchPredictionProcessCompleted(context, (SearchIdentifier: trackingSearchIdentifier, OriginalSearchIdentifier: originalSearchIdentifier,
                        FailureInfo: matchPredictionFailureInfo, DonorsPerBatch: matchPredictionProcessingBatchSize, TotalNumberOfBatches: matchPredictionNumberOfBatches));
                }
            }
        }

        [Function(nameof(RepeatSearchOrchestrator))]
        public async Task<SearchOrchestrationOutput> RepeatSearchOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
        {
            var notification = context.GetInput<MatchingResultsNotification>();
            var requestInfo = mapper.Map<FailureNotificationRequestInfo>(notification);
            var requestCompletedSuccessfully = false;
            var orchestrationStartTime = context.CurrentUtcDateTime;
            TimedResultSet<IReadOnlyDictionary<int, string>> matchPredictionResultLocations = null;
            int? matchPredictionNumberOfBatches = null;
            MatchPredictionFailureInfo matchPredictionFailureInfo = null;

            var trackingSearchIdentifier = new Guid(requestInfo.RepeatSearchRequestId);
            var originalSearchIdentifier = new Guid(requestInfo.SearchRequestId);
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

                await SendMatchPredictionProcessInitiated(context, (SearchIdentifier: trackingSearchIdentifier, OriginalSearchIdentifier: originalSearchIdentifier,  InitiationTimeUtc: orchestrationStartTime));

                var matchPredictionRequestLocations = await PrepareMatchPrediction(context, notification, requestInfo);
                await SendMatchPredictionBatchProcessingStarted(context, (SearchIdentifier: trackingSearchIdentifier, OriginalSearchIdentifier: originalSearchIdentifier));
                (matchPredictionResultLocations, matchPredictionNumberOfBatches)
                    = await RunMatchPredictionAlgorithm(context, requestInfo, matchPredictionRequestLocations);

                await SendMatchPredictionBatchProcessingEnded(context, (SearchIdentifier: trackingSearchIdentifier, OriginalSearchIdentifier: originalSearchIdentifier));

                await PersistSearchResults(
                    context,
                    new PersistSearchResultsFunctionParameters
                    {
                        SearchInitiated = orchestrationInitiated,
                        MatchingResultsNotification = notification,
                        MatchPredictionResultLocations = matchPredictionResultLocations
                    },
                    requestInfo);

                requestCompletedSuccessfully = true;

                // "return" populates the "output" property on the status check GET endpoint set up by the durable functions framework
                return new SearchOrchestrationOutput
                {
                    TotalSearchTime = context.CurrentUtcDateTime.Subtract(orchestrationInitiated),
                    MatchingDonorCount = notification.NumberOfResults ?? -1,
                };
            }
            catch (HandledOrchestrationException e)
            {
                // Exceptions wrapper in "HandleOrchestrationException" have already been handled, and failure notifications sent.
                // In this case we should just re-throw so the function is tracked as a failure.

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
                logger.SendTrace($"Failure during orchestration. Exception: {e.Message}, {e.InnerException?.Message}");

                // An unexpected exception occurred in the *orchestration* code. Ensure we send a failure notification
                requestInfo.StageReached = "Orchestrator";
                await SendFailureNotification(context, requestInfo);

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
                await SendMatchPredictionProcessCompleted(context, (SearchIdentifier: trackingSearchIdentifier, OriginalSearchIdentifier: originalSearchIdentifier,
                    FailureInfo: matchPredictionFailureInfo, DonorsPerBatch: matchPredictionProcessingBatchSize, TotalNumberOfBatches: matchPredictionNumberOfBatches));
            }
        }

        private async Task<TimedResultSet<IList<string>>> PrepareMatchPrediction(
            TaskOrchestrationContext context,
            MatchingResultsNotification notification,
            FailureNotificationRequestInfo requestInfo)
        {
            requestInfo.StageReached = nameof(SearchActivityFunctions.PrepareMatchPredictionBatches);

            var batchIds = await RunStageAndHandleFailures(async () =>
                    await context.CallActivityAsync<TimedResultSet<IList<string>>>(
                        nameof(SearchActivityFunctions.PrepareMatchPredictionBatches),
                        notification,
                        RetryOptions
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

        private async Task<(TimedResultSet<IReadOnlyDictionary<int, string>> TimedResultSets, int MatchPredictionNumberOfBatches)> RunMatchPredictionAlgorithm(
            TaskOrchestrationContext context,
            FailureNotificationRequestInfo requestInfo,
            TimedResultSet<IList<string>> matchPredictionRequestLocations
        )
        {
            requestInfo.StageReached = nameof(RunMatchPredictionAlgorithm);

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

            var timedResultSet = new TimedResultSet<IReadOnlyDictionary<int, string>>
            {
                ResultSet = MoreLinq.Extensions.ToDictionaryExtension.ToDictionary(matchPredictionResultLocations),
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
            FailureNotificationRequestInfo requestInfo
        )
        {
            requestInfo.StageReached = nameof(PersistSearchResults);

            await RunStageAndHandleFailures(
                async () => await context.CallActivityAsync(nameof(SearchActivityFunctions.PersistSearchResults), parameters,
                RetryOptions),
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
            TaskOrchestrationContext context,
            FailureNotificationRequestInfo requestInfo) =>
            await RunStageAndHandleFailures(async () =>
            {
                await runStage();
                return true;
            }, context, requestInfo);

        private async Task<T> RunStageAndHandleFailures<T>(
            Func<Task<T>> runStage,
            TaskOrchestrationContext context,
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
            TaskOrchestrationContext context,
            FailureNotificationRequestInfo requestInfo)
        {
            await context.CallActivityAsync(
                nameof(SearchActivityFunctions.SendFailureNotification),
                requestInfo,
                RetryOptions
            );

            context.SetCustomStatus($"Search failed, during stage: {requestInfo.StageReached}");
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
            (Guid SearchIdentifier, Guid? OriginalSearchIdentifier, DateTime InitiationTimeUtc) eventDetails) =>
            await context.CallActivityAsync(
                nameof(SearchActivityFunctions.SendMatchPredictionProcessInitiated),
                eventDetails,
                RetryOptions
            );

        private static async Task SendMatchPredictionBatchProcessingStarted(
            TaskOrchestrationContext context,
            (Guid SearchIdentifier, Guid? OriginalSearchIdentifier) eventDetails) =>
            await context.CallActivityAsync(
                nameof(SearchActivityFunctions.SendMatchPredictionBatchProcessingStarted),
                eventDetails,
                RetryOptions
            );

        private static async Task SendMatchPredictionBatchProcessingEnded(
            TaskOrchestrationContext context,
            (Guid SearchIdentifier, Guid? OriginalSearchIdentifier) eventDetails) =>
            await context.CallActivityAsync(
                nameof(SearchActivityFunctions.SendMatchPredictionBatchProcessingEnded),
                eventDetails,
                RetryOptions
            );

        private static async Task SendMatchPredictionProcessCompleted(
            TaskOrchestrationContext context,
            (Guid SearchIdentifier, Guid? OriginalSearchIdentifier, MatchPredictionFailureInfo FailureInfo, int? DonorsPerBatch, int? TotalNumberOfBatches) eventDetails) =>
            await context.CallActivityAsync(
                nameof(SearchActivityFunctions.SendMatchPredictionProcessCompleted),
                eventDetails,
                RetryOptions
            );
    }
}