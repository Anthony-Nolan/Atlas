using System;
using System.Collections.Generic;
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
    private readonly int abandonBatchAfterMinutes;
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
        abandonBatchAfterMinutes = orchestrationSettings.Value.AbandonBatchAfterMinutes;
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
    /// <see cref="IParallelMatchPredictionCompletionService.FinaliseRun"/>. The lease prevents concurrent invocations
    /// from processing the same run: only the invocation that wins the compare-and-swap claim will proceed.
    /// On failure the completion service moves the run to
    /// <see cref="Atlas.MatchPrediction.Data.Models.ParallelMatchPredictionRunStatus.FailedDuringCompletion"/>
    /// and rethrows; this trigger collects the exceptions so other runs in the same tick still get processed, then
    /// re-throws them as an <see cref="AggregateException"/> at the end so the invocation is recorded as Failed.
    /// </summary>
    [Function(nameof(FinaliseCompletedParallelMatchPredictionRuns))]
    public async Task FinaliseCompletedParallelMatchPredictionRuns(
        [TimerTrigger("%AtlasFunction:Orchestration:ParallelFinalisationCronSchedule%")]
        TimerInfo _)
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

        var failures = new List<Exception>();
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
                await completionService.FinaliseRun(runId);
            }
            catch (Exception ex)
            {
                // Run is already marked FailedDuringCompletion by the completion service — log and continue so a
                // single failure does not block finalising the remaining runs in this tick.
                logger.LogError(ex, "Parallel match prediction run {RunId} failed during completion and has been marked as FailedDuringCompletion.",
                    runId
                );
                failures.Add(new InvalidOperationException($"Failed to finalise parallel match prediction run {runId}.", ex));
            }
        }

        // Re-throw the collected failures so the invocation is recorded as Failed for monitoring/alerting. Each failed
        // run is already marked FailedDuringCompletion, so it will not be re-selected by the next sweep.
        if (failures.Count > 0)
        {
            throw new AggregateException(
                $"{failures.Count} of {runIdsAwaitingFinalisation.Count} parallel match prediction run(s) failed to finalise in this sweep.",
                failures
            );
        }
    }

    /// <summary>
    /// Timer-triggered abandonment sweep. Finds runs whose batches were initiated more than
    /// <c>AbandonBatchAfterMinutes</c> minutes ago but that still have un-returned batches, then for each one marks the
    /// run and its missing batches as
    /// <see cref="Atlas.MatchPrediction.Data.Models.ParallelMatchPredictionRunStatus.Abandoned"/> and publishes a
    /// failure notification (<c>WasSuccessful=false</c>) downstream. The per-batch rows are retained for research until
    /// the clean-up timer purges them. If the missing batch results arrive later (before cleanup) the finaliser replays
    /// the run to <see cref="Atlas.MatchPrediction.Data.Models.ParallelMatchPredictionRunStatus.Finalised"/>.
    /// </summary>
    [Function(nameof(MarkRunsAsAbandoned))]
    public async Task MarkRunsAsAbandoned(
        [TimerTrigger("%AtlasFunction:Orchestration:ParallelBatchAbandonmentCronSchedule%")]
        TimerInfo _)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-abandonBatchAfterMinutes);
        var runIdsToAbandon = await repository.GetRunIdsToAbandon(cutoff);
        if (runIdsToAbandon.Count == 0)
        {
            return;
        }

        logger.LogWarning(
            "Found {Count} parallel match prediction run(s) to abandon (no batch results received within {Minutes} minute(s) of initiation).",
            runIdsToAbandon.Count, abandonBatchAfterMinutes
        );

        var failures = new List<Exception>();
        foreach (var runId in runIdsToAbandon)
        {
            try
            {
                await completionService.AbandonRun(runId);
            }
            catch (Exception ex)
            {
                // Log and continue so a single failure does not block abandoning the remaining runs in this tick.
                logger.LogError(ex, "Failed to abandon parallel match prediction run {RunId}.", runId);
                failures.Add(new InvalidOperationException($"Failed to abandon parallel match prediction run {runId}.", ex));
            }
        }

        // Every run above was still attempted; a run that failed here stays Running and is re-selected on the next
        // sweep (AbandonRun is an idempotent status compare-and-swap, so a retry cannot double-notify). Re-throw the
        // collected failures so the invocation is recorded as Failed for monitoring/alerting and is easy to research —
        // a swallowed abandonment can otherwise leave no durable trace beyond this log line.
        if (failures.Count > 0)
        {
            throw new AggregateException(
                $"{failures.Count} of {runIdsToAbandon.Count} parallel match prediction run(s) failed to abandon in this sweep.",
                failures
            );
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
        TimerInfo _)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        var deletedBatchesCount = await repository.CleanupBatchesForRunsInitiatedBefore(cutoffDate);
        logger.LogInformation(
            "Parallel match prediction batch cleanup deleted {Count} batch row(s) for runs initiated before {Cutoff:o} (retention {Days} day(s)) and marked their parent runs as cleaned up (IsCleanedUp=true).",
            deletedBatchesCount, cutoffDate, retentionDays
        );
    }
}