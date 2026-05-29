using System;
using System.Threading.Tasks;
using Atlas.Functions.Services;
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
        IOptions<Settings.OrchestrationSettings> orchestrationSettings,
        ILogger<ParallelMatchPredictionAggregatorFunctions> logger)
    {
        this.repository = repository;
        this.completionService = completionService;
        retentionDays = orchestrationSettings.Value.ParallelBatchRetentionDays;
        this.logger = logger;
    }

    /// <summary>
    /// Session-aware Service Bus trigger. Persists a single batch result row keyed by
    /// <c>(RunId, BatchSequenceNumber)</c>. Duplicate Service Bus deliveries are silently ignored at the
    /// repository level (idempotency is enforced by the unique index, not by the function).
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
        var inserted = await repository.RecordBatchResult(
            message.ParallelRunId,
            message.BatchSequenceNumber,
            message.MatchPredictionResultLocations
        );

        if (inserted)
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

    /// <summary>
    /// Timer-triggered finaliser. Scans for runs that have received all expected batches and are not yet
    /// finalised (and not currently held by a live finalisation lease), then drives the persistence pipeline for
    /// each via <see cref="IParallelMatchPredictionCompletionService.CompleteRun"/>. Runs concurrently with the
    /// batch-store trigger; each run is claimed with an atomic lease so duplicate work cannot happen, and a run is
    /// only marked finalised after its pipeline succeeds.
    /// </summary>
    [Function(nameof(FinaliseCompletedParallelMatchPredictionRuns))]
    public async Task FinaliseCompletedParallelMatchPredictionRuns(
        [TimerTrigger("%AtlasFunction:Orchestration:ParallelFinalisationCronSchedule%")]
        TimerInfo timer)
    {
        var runIdsAwaitingFinalisation = await repository.GetRunIdsAwaitingFinalisation(DateTime.UtcNow);
        if (runIdsAwaitingFinalisation.Count == 0)
        {
            return;
        }

        logger.LogInformation(
            "Found {Count} parallel match prediction run(s) awaiting finalisation.",
            runIdsAwaitingFinalisation.Count
        );

        foreach (var runId in runIdsAwaitingFinalisation)
        {
            try
            {
                await completionService.CompleteRun(runId);
            }
            catch (Exception ex)
            {
                // Don't let a single bad run stop the rest of the batch — log and continue.
                logger.LogError(ex,
                    "Failed to finalise parallel match prediction run {RunId}; will retry on next timer tick if lease allows.",
                    runId
                );
            }
        }
    }

    /// <summary>
    /// Timer-triggered clean-up. Deletes per-batch rows belonging to runs that finalised more than
    /// <c>ParallelBatchRetentionDays</c> ago. The parent <c>ParallelMatchPredictionRun</c> rows are kept
    /// indefinitely so historic searches remain visible in dashboards/audits.
    /// </summary>
    [Function(nameof(CleanupOldParallelMatchPredictionBatches))]
    public async Task CleanupOldParallelMatchPredictionBatches(
        [TimerTrigger("%AtlasFunction:Orchestration:ParallelBatchCleanupCronSchedule%")]
        TimerInfo timer)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        var deletedBatchesCount = await repository.DeleteBatchesForRunsFinalisedBefore(cutoffDate);
        logger.LogInformation(
            "Parallel match prediction batch cleanup deleted {Count} batch row(s) finalised before {Cutoff:o} (retention {Days} day(s)).",
            deletedBatchesCount, cutoffDate, retentionDays
        );
    }
}