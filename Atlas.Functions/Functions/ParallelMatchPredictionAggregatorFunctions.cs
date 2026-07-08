using System;
using System.Threading.Tasks;
using Atlas.Functions.Services;
using Atlas.Functions.Settings;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Atlas.Functions.Functions;

/// <summary>
/// Aggregator-side functions for the parallel match-prediction path.
///
/// The work is intentionally split across three triggers to avoid the race condition the previous
/// "all-in-one" function had (two concurrently-arriving final batches both observing
/// <c>processed &lt; total</c>):
/// </summary>
public class ParallelMatchPredictionAggregatorFunctions
{
    private readonly IParallelMatchPredictionRepository repository;
    private readonly IParallelMatchPredictionCompletionService completionService;
    private readonly int retentionDays;
    private readonly ILogger<ParallelMatchPredictionAggregatorFunctions> logger;

    public ParallelMatchPredictionAggregatorFunctions(
        IParallelMatchPredictionRepository repository,
        IParallelMatchPredictionCompletionService completionService,
        IOptions<OrchestrationSettings> orchestrationSettings,
        ILogger<ParallelMatchPredictionAggregatorFunctions> logger)
    {
        this.repository = repository;
        this.completionService = completionService;
        retentionDays = orchestrationSettings.Value.ParallelBatchRetentionDays;
        this.logger = logger;
    }

    /// <summary>
    /// Session-aware Service Bus trigger. Persists a single batch result row keyed by <c>BatchId</c>; duplicate
    /// deliveries are ignored via the batch status. Failure results are recorded as <c>Failed</c> so the run can
    /// still be finalised.
    /// </summary>
    [Function(nameof(StoreParallelMatchPredictionBatchResult))]
    public async Task StoreParallelMatchPredictionBatchResult(
        [ServiceBusTrigger(
            "%AtlasFunction:MessagingServiceBus:ParallelMatchPredictionResultsTopic%",
            "%AtlasFunction:MessagingServiceBus:ParallelMatchPredictionResultsSubscription%",
            Connection = "AtlasFunction:MessagingServiceBus:ConnectionString",
            IsSessionsEnabled = true
        )]
        ParallelMatchPredictionBatchResult message)
    {

        if (message.IsSuccessful)
        {
            var wasBatchResultRecordedSuccessfully = await repository.RecordBatchResult(
                message.BatchId,
                message.MatchPredictionResultLocation
            );

            if (wasBatchResultRecordedSuccessfully)
            {
                logger.LogInformation(
                    "Recorded parallel MPA batch result. BatchId={BatchId}, RunId={RunId}, BatchSequenceNumber={BatchSequenceNumber}, Search={SearchIdentifier}.",
                    message.BatchId, message.ParallelRunId, message.BatchSequenceNumber, message.SearchIdentifier
                );
            }
            else
            {
                logger.LogWarning(
                    "Duplicate parallel MPA batch result ignored. BatchId={BatchId}, RunId={RunId}, BatchSequenceNumber={BatchSequenceNumber}, Search={SearchIdentifier}.",
                    message.BatchId, message.ParallelRunId, message.BatchSequenceNumber, message.SearchIdentifier
                );
            }
        }
        else
        {
            var wasFailureRecordedSuccessfully = await repository.RecordBatchFailure(
                message.BatchId,
                message.FailureMessage,
                message.FailureException
            );

            if (wasFailureRecordedSuccessfully)
            {
                logger.LogError(
                    "Recorded parallel MPA batch failure. BatchId={BatchId}, RunId={RunId}, BatchSequenceNumber={BatchSequenceNumber}, Search={SearchIdentifier}. Failure: {FailureMessage}",
                    message.BatchId, message.ParallelRunId, message.BatchSequenceNumber, message.SearchIdentifier, message.FailureMessage
                );
            }
            else
            {
                logger.LogWarning(
                    "Duplicate parallel MPA batch failure ignored. BatchId={BatchId}, RunId={RunId}, BatchSequenceNumber={BatchSequenceNumber}, Search={SearchIdentifier}.",
                    message.BatchId, message.ParallelRunId, message.BatchSequenceNumber, message.SearchIdentifier
                );
            }
        }
    }

    /// <summary>
    /// Timer-triggered finaliser. Scans for unclaimed runs that have received all expected batches and are still in the
    /// <see cref="Atlas.MatchPrediction.Data.Models.ParallelMatchPredictionRunStatus.Running"/> state, then atomically
    /// claims each run with a per-invocation lease GUID before driving the persistence pipeline via
    /// <see cref="IParallelMatchPredictionCompletionService.CompleteRun"/>. The lease prevents concurrent invocations
    /// from processing the same run: only the invocation that wins the compare-and-swap claim will proceed.
    /// On failure the completion service moves the run to
    /// <see cref="Atlas.MatchPrediction.Data.Models.ParallelMatchPredictionRunStatus.FailedDuringCompletion"/>
    /// and rethrows; this trigger swallows the exception so other runs in the same tick still get processed.
    /// </summary>
    [Function(nameof(FinaliseCompletedParallelMatchPredictionRuns))]
    public async Task FinaliseCompletedParallelMatchPredictionRuns(
        [TimerTrigger("%AtlasFunction:Orchestration:ParallelFinalisationCronSchedule%")]
        TimerInfo timer)
    {
        var runIdsAwaitingFinalisation = await repository.GetRunIdsAwaitingFinalisationAndNotLeased();
        if (runIdsAwaitingFinalisation.Count == 0)
        {
            return;
        }

        logger.LogInformation(
            "Found {Count} parallel match prediction run(s) awaiting finalisation.",
            runIdsAwaitingFinalisation.Count
        );

        // A unique id for this invocation — used as the lease owner so concurrent scheduled runs
        // cannot claim and double-process the same match-prediction run.
        var invocationLeaseOwner = Guid.NewGuid();

        foreach (var runId in runIdsAwaitingFinalisation)
        {
            var claimed = await repository.TryClaimFinalisationLease(runId, invocationLeaseOwner);
            if (!claimed)
            {
                logger.LogInformation(
                    "Parallel match prediction run {RunId} was already claimed by another invocation; skipping.",
                    runId
                );
                continue;
            }

            try
            {
                await completionService.CompleteRun(runId);
            }
            catch (Exception ex)
            {
                // Run is already marked FailedDuringCompletion by the completion service — log and move on.
                logger.LogError(ex,
                    "Parallel match prediction run {RunId} failed during completion and has been marked as FailedDuringCompletion.",
                    runId
                );
            }
        }
    }

    /// <summary>
    /// Timer-triggered clean-up. Deletes per-batch rows belonging to <em>any</em> run that has been in the
    /// database for more than <c>ParallelBatchRetentionDays</c> (measured from <c>MatchPredictionRunInitiatedUtc</c>),
    /// regardless of status — this includes abandoned runs still in
    /// <see cref="Atlas.MatchPrediction.Data.Models.ParallelMatchPredictionRunStatus.Running"/>,
    /// finalised runs, and failed runs — and marks each such run with
    /// <see cref="Atlas.MatchPrediction.Data.Models.ParallelMatchPredictionRun.IsCleanedUp"/> = <c>true</c>.
    /// The run's <c>Status</c> is left unchanged. The parent <c>ParallelMatchPredictionRun</c> rows are kept
    /// indefinitely so historic searches remain visible in dashboards/audits.
    /// </summary>
    [Function(nameof(CleanupOldParallelMatchPredictionBatches))]
    public async Task CleanupOldParallelMatchPredictionBatches(
        [TimerTrigger("%AtlasFunction:Orchestration:ParallelBatchCleanupCronSchedule%")]
        TimerInfo timer)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        var deletedBatchesCount = await repository.CleanupBatchesForRunsInitiatedBefore(cutoffDate);
        logger.LogInformation(
            "Parallel match prediction batch cleanup deleted {Count} batch row(s) for runs initiated before {Cutoff:o} (retention {Days} day(s)) and marked their parent runs as cleaned up (IsCleanedUp=true).",
            deletedBatchesCount, cutoffDate, retentionDays
        );
    }
}