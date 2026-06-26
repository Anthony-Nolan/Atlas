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
    /// Session-aware Service Bus trigger. Persists a single batch result row keyed by
    /// <c>(RunId, BatchSequenceNumber)</c>. Duplicate Service Bus deliveries are silently ignored at the
    /// repository level (idempotency is enforced by the unique index, not by the function).
    /// Failure results (<see cref="ParallelMatchPredictionBatchResult.IsSuccessful"/> == <c>false</c>) record
    /// the batch as <c>Failed</c> so the run can still be finalised even when some batches fail.
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
                message.ParallelRunId,
                message.BatchSequenceNumber,
                message.MatchPredictionResultLocations
            );

            if (wasBatchResultRecordedSuccessfully)
            {
                logger.LogInformation(
                    "Recorded parallel MPA batch result. RunId={RunId}, BatchSequenceNumber={BatchSequenceNumber}, Search={SearchIdentifier}.",
                    message.ParallelRunId, message.BatchSequenceNumber, message.SearchIdentifier
                );
            }
            else
            {
                logger.LogWarning(
                    "Duplicate parallel MPA batch result ignored. RunId={RunId}, BatchSequenceNumber={BatchSequenceNumber}, Search={SearchIdentifier}.",
                    message.ParallelRunId, message.BatchSequenceNumber, message.SearchIdentifier
                );
            }
        }
        else
        {
            var wasFailureRecordedSuccessfully = await repository.RecordBatchFailure(
                message.ParallelRunId,
                message.BatchSequenceNumber,
                message.FailureMessage,
                message.FailureException
            );

            if (wasFailureRecordedSuccessfully)
            {
                logger.LogError(
                    "Recorded parallel MPA batch failure. RunId={RunId}, BatchSequenceNumber={BatchSequenceNumber}, Search={SearchIdentifier}. Failure: {FailureMessage}",
                    message.ParallelRunId, message.BatchSequenceNumber, message.SearchIdentifier, message.FailureMessage
                );
            }
            else
            {
                logger.LogWarning(
                    "Duplicate parallel MPA batch failure ignored. RunId={RunId}, BatchSequenceNumber={BatchSequenceNumber}, Search={SearchIdentifier}.",
                    message.ParallelRunId, message.BatchSequenceNumber, message.SearchIdentifier
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
                logger.LogError(ex, "Parallel match prediction run {RunId} failed during completion and has been marked as FailedDuringCompletion.",
                    runId
                );
            }
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
            }
        }
    }

    /// <summary>
    /// Timer-triggered clean-up. Deletes per-batch rows belonging to runs that finalised more than
    /// <c>ParallelBatchRetentionDays</c> ago (marking them
    /// <see cref="Atlas.MatchPrediction.Data.Models.ParallelMatchPredictionRunStatus.FinalisedAndCleanedUp"/>) and,
    /// using the same retention period, runs that were abandoned more than <c>ParallelBatchRetentionDays</c> ago
    /// (marking them <see cref="Atlas.MatchPrediction.Data.Models.ParallelMatchPredictionRunStatus.AbandonedAndCleanedUp"/>).
    /// The parent <c>ParallelMatchPredictionRun</c> rows are kept indefinitely so historic searches remain visible
    /// in dashboards/audits.
    /// </summary>
    [Function(nameof(CleanupOldParallelMatchPredictionBatches))]
    public async Task CleanupOldParallelMatchPredictionBatches(
        [TimerTrigger("%AtlasFunction:Orchestration:ParallelBatchCleanupCronSchedule%")]
        TimerInfo _)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

        var deletedFinalisedBatchesCount = await repository.CleanupBatchesForRunsFinalisedBefore(cutoffDate);
        var deletedAbandonedBatchesCount = await repository.CleanupBatchesForRunsAbandonedBefore(cutoffDate);

        logger.LogInformation(
            "Parallel match prediction batch cleanup deleted {FinalisedCount} batch row(s) from finalised runs and "
          + "{AbandonedCount} batch row(s) from abandoned runs before {Cutoff:o} (retention {Days} day(s)), and marked "
          + "their parent runs as cleaned up.",
            deletedFinalisedBatchesCount, deletedAbandonedBatchesCount, cutoffDate, retentionDays
        );
    }
}