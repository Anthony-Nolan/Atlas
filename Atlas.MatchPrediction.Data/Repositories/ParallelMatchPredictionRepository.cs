using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.Data.Context;
using Atlas.MatchPrediction.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Atlas.MatchPrediction.Data.Repositories;

public record CreateParallelMatchPredictionRunInfo(
    Guid SearchIdentifier,
    bool IsRepeatSearch,
    Guid? RepeatSearchIdentifier,
    string ResultsFileName,
    bool ResultsBatched,
    string BatchFolderName,
    TimeSpan MatchingAlgorithmElapsedTime,
    DateTime SearchInitiatedTimeUtc,
    int TotalBatchCount);

/// <summary>
/// Minimal identifying information about a run that has just been abandoned — enough to publish the downstream
/// failure notification without reloading the full run and its batches.
/// </summary>
public record AbandonedRunHeader(Guid SearchIdentifier, Guid? RepeatSearchIdentifier, bool IsRepeatSearch, int TotalBatchCount);

/// <summary>
/// The new run id together with the ids of its pre-created batch rows, indexed by batch sequence number
/// (<c>BatchIdsBySequence[seq]</c> is the id of the batch with <c>BatchSequenceNumber == seq</c>).
/// </summary>
public record CreateParallelMatchPredictionRunResult(int RunId, IReadOnlyList<int> BatchIdsBySequence);

/// <summary>A run together with the result-file locations of its received batches, plus any failed batches.</summary>
public class ParallelMatchPredictionRunResults
{
    public ParallelMatchPredictionRun Run { get; init; }

    /// <summary>Blob filenames of the result files for every batch with a non-null location.</summary>
    public IReadOnlyList<string> BatchResultLocations { get; init; }

    /// <summary>Batches whose <see cref="ParallelMatchPredictionBatch.BatchStatus"/> is <see cref="ParallelMatchPredictionBatchStatus.Failed"/>.</summary>
    public IReadOnlyList<ParallelMatchPredictionBatch> FailedBatches { get; init; }
}

public interface IParallelMatchPredictionRepository
{
    /// <summary>
    /// Creates the parent run record and pre-creates one <see cref="ParallelMatchPredictionBatch"/> row per expected
    /// batch (<c>BatchSequenceNumber</c> 0 … <c>TotalBatchCount − 1</c>) before any batch messages are dispatched.
    /// </summary>
    Task<CreateParallelMatchPredictionRunResult> CreateRun(CreateParallelMatchPredictionRunInfo info);

    /// <summary>
    /// Records a successful batch result by updating the pre-created batch row identified by <paramref name="batchId"/>.
    /// Idempotent — a duplicate delivery is a no-op returning <c>false</c>.
    /// </summary>
    /// <returns><c>true</c> on first record; <c>false</c> if it was a duplicate.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no batch row exists for the given id.</exception>
    Task<bool> RecordBatchResult(int batchId, string resultLocation);

    /// <summary>
    /// Records a batch failure by updating the pre-created batch row identified by <paramref name="batchId"/>.
    /// Idempotent — a duplicate delivery is a no-op returning <c>false</c>.
    /// </summary>
    /// <returns><c>true</c> on first record; <c>false</c> if it was a duplicate.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no batch row exists for the given id.</exception>
    Task<bool> RecordBatchFailure(int batchId, string failureMessage, string failureException);

    /// <summary>
    /// Returns the ids of all <see cref="ParallelMatchPredictionRunStatus.Running"/> (or replayed
    /// <see cref="ParallelMatchPredictionRunStatus.Abandoned"/>) runs whose every batch has a
    /// <see cref="ParallelMatchPredictionBatch.BatchStatus"/> other than
    /// <see cref="ParallelMatchPredictionBatchStatus.Requested"/> or <see cref="ParallelMatchPredictionBatchStatus.Abandoned"/>
    /// (i.e. every batch has a result, success or failure) <em>and</em> that have not yet been claimed by another
    /// invocation (i.e. <see cref="ParallelMatchPredictionRun.FinalisationLeaseOwner"/> is <c>null</c>).
    /// Intended for the finalisation timer.
    /// </summary>
    Task<IReadOnlyList<int>> GetRunIdsAwaitingFinalisationAndNotLeased();

    /// <summary>
    /// Attempts to atomically claim the given run for finalisation by this invocation.
    /// Sets <see cref="ParallelMatchPredictionRun.FinalisationLeaseOwner"/> to <paramref name="leaseOwner"/>
    /// only when the run has status <see cref="ParallelMatchPredictionRunStatus.Running"/> or
    /// <see cref="ParallelMatchPredictionRunStatus.Abandoned"/> (replay) and its
    /// <see cref="ParallelMatchPredictionRun.FinalisationLeaseOwner"/> is currently <c>null</c>.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the lease was successfully acquired by this call; <c>false</c> if another invocation
    /// already holds the lease or the run is no longer in the <c>Running</c>/<c>Abandoned</c> state.
    /// </returns>
    Task<bool> TryClaimFinalisationLease(int runId, Guid leaseOwner);

    /// <summary>
    /// Returns the run together with the blob locations of each successfully-received batch's result file.
    /// Returns <c>null</c> if the run does not exist.
    /// </summary>
    Task<ParallelMatchPredictionRunResults> GetRunWithResults(int runId);

    /// <summary>
    /// Marks the run as <see cref="ParallelMatchPredictionRunStatus.Finalised"/> and sets
    /// <see cref="ParallelMatchPredictionRun.IsSuccessful"/> to <c>true</c>, provided it still has
    /// status <see cref="ParallelMatchPredictionRunStatus.Running"/> or
    /// <see cref="ParallelMatchPredictionRunStatus.Abandoned"/>. The Abandoned case is the late-result replay:
    /// an abandoned run keeps its run-status while its per-batch rows are superseded by late results, and once no
    /// <see cref="ParallelMatchPredictionBatchStatus.Requested"/>/<see cref="ParallelMatchPredictionBatchStatus.Abandoned"/>
    /// batch remains the finaliser re-picks it and flips it to Finalised here.
    /// Call this as the very last step, after all persistence has succeeded.
    /// </summary>
    Task MarkRunFinalised(int runId, DateTime finalisedTimeUtc);

    /// <summary>
    /// Marks the run as <see cref="ParallelMatchPredictionRunStatus.FailedDuringBatchProcessing"/> and sets
    /// <see cref="ParallelMatchPredictionRun.IsSuccessful"/> to <c>false</c> when one or more batches
    /// failed during Worker processing. The completion pipeline still runs (metrics, notification, tracking).
    /// </summary>
    Task MarkRunFailed(int runId, DateTime nowUtc);

    /// <summary>
    /// Marks the run as <see cref="ParallelMatchPredictionRunStatus.FailedDuringCompletion"/> when the
    /// persistence pipeline throws. Terminal: the finalisation timer will not re-pick the run.
    /// </summary>
    Task MarkRunFailedDuringCompletion(int runId, DateTime nowUtc);

    /// <summary>
    /// Deletes batch rows belonging to runs that have not yet been cleaned up
    /// (<see cref="ParallelMatchPredictionRun.IsCleanedUp"/> is <c>false</c>) and whose
    /// <c>MatchPredictionRunInitiatedUtc</c> is before <paramref name="cutoffUtc"/> — i.e. every run older than the
    /// retention period, whatever its outcome (<see cref="ParallelMatchPredictionRunStatus.Finalised"/>,
    /// FailedDuring*, <see cref="ParallelMatchPredictionRunStatus.Abandoned"/>, or a still-<see cref="ParallelMatchPredictionRunStatus.Running"/>
    /// run that never completed). The one exception is a Running run currently claimed for finalisation
    /// (<see cref="ParallelMatchPredictionRun.FinalisationLeaseOwner"/> set): its batches are left in place so the
    /// in-flight completion pipeline does not read an empty batch set. Marks the cleaned runs by setting
    /// <see cref="ParallelMatchPredictionRun.IsCleanedUp"/> to <c>true</c>; <see cref="ParallelMatchPredictionRun.Status"/>
    /// is left unchanged so the run stays a historical record. Batch deletion and the flag update run in a single
    /// transaction. Parent run rows are intentionally retained.
    /// </summary>
    /// <returns>The number of batch rows deleted.</returns>
    Task<int> CleanupBatchesForRunsInitiatedBefore(DateTime cutoffUtc);

    /// <summary>
    /// Returns the ids of runs that should be abandoned: still <see cref="ParallelMatchPredictionRunStatus.Running"/>,
    /// initiated before <paramref name="cutoffUtc"/>, not currently leased by a finaliser invocation
    /// (<see cref="ParallelMatchPredictionRun.FinalisationLeaseOwner"/> is <c>null</c>), and with at least one batch
    /// still in the <see cref="ParallelMatchPredictionBatchStatus.Requested"/> state (i.e. a result never arrived).
    /// </summary>
    Task<IReadOnlyList<int>> GetRunIdsToAbandon(DateTime cutoffUtc);

    /// <summary>
    /// Atomically transitions a single <see cref="ParallelMatchPredictionRunStatus.Running"/> run to
    /// <see cref="ParallelMatchPredictionRunStatus.Abandoned"/> (setting <see cref="ParallelMatchPredictionRun.IsSuccessful"/>
    /// to <c>false</c>) and marks its still-<see cref="ParallelMatchPredictionBatchStatus.Requested"/> batches as
    /// <see cref="ParallelMatchPredictionBatchStatus.Abandoned"/>. The status compare-and-swap is the concurrency guard:
    /// only the first caller wins, and the run is only abandoned while <see cref="ParallelMatchPredictionRun.FinalisationLeaseOwner"/>
    /// is <c>null</c> — a run a finaliser has already claimed (e.g. because a late batch result arrived after selection)
    /// is left to that finaliser rather than being abandoned. Batches that already have a result are left untouched for
    /// research, and the lease is itself left <c>null</c> here so a late-result replay can later finalise the run.
    /// </summary>
    /// <returns>The run header if this call performed the transition; <c>null</c> if the run was no longer Running or had already been leased.</returns>
    Task<AbandonedRunHeader> TryMarkRunAsAbandoned(int runId, DateTime nowUtc);

    /// <summary>
    /// Marks a run as failed during dispatch: while the run is still <see cref="ParallelMatchPredictionRunStatus.Running"/>
    /// (a compare-and-swap guard), <see cref="ParallelMatchPredictionRun.IsSuccessful"/> is set to <c>false</c>;
    /// <see cref="ParallelMatchPredictionRun.Status"/> is left as <see cref="ParallelMatchPredictionRunStatus.Running"/> for the finaliser.
    /// Only batches still in the <see cref="ParallelMatchPredictionBatchStatus.Requested"/> state are set to
    /// <see cref="ParallelMatchPredictionBatchStatus.Failed"/> with the dispatch-failure detail — batches already reported by the
    /// Worker (a partial dispatch can leave some dispatched and reported before the failure) are left intact.
    /// <see cref="ParallelMatchPredictionBatch.ResultReceivedTimeUtc"/> is deliberately left <c>null</c> (no result was received);
    /// only <see cref="ParallelMatchPredictionBatch.BatchStatusDate"/> is stamped.
    /// Leaving the run Running with no Requested batches makes it immediately eligible for the finalisation timer, which performs the full
    /// downstream failure processing and transitions the run to <see cref="ParallelMatchPredictionRunStatus.FailedDuringBatchProcessing"/>.
    /// <paramref name="failureMessage"/> is truncated to the column limit (1024).
    /// </summary>
    Task MarkRunAsDispatchFailed(int runId, string failureMessage, string failureException, DateTime nowUtc);
}

public class ParallelMatchPredictionRepository : IParallelMatchPredictionRepository
{
    private readonly MatchPredictionContext context;

    public ParallelMatchPredictionRepository(MatchPredictionContext context)
    {
        this.context = context;
    }

    public async Task<CreateParallelMatchPredictionRunResult> CreateRun(CreateParallelMatchPredictionRunInfo info)
    {
        return await context.ExecuteInTransactionAsync(async () =>
            {
                var now = DateTime.UtcNow;
                var entity = new ParallelMatchPredictionRun
                {
                    SearchIdentifier = info.SearchIdentifier,
                    IsRepeatSearch = info.IsRepeatSearch,
                    RepeatSearchIdentifier = info.RepeatSearchIdentifier,
                    ResultsFileName = info.ResultsFileName,
                    ResultsBatched = info.ResultsBatched,
                    BatchFolderName = info.BatchFolderName,
                    MatchingAlgorithmElapsedTime = info.MatchingAlgorithmElapsedTime,
                    SearchInitiatedTimeUtc = info.SearchInitiatedTimeUtc,
                    TotalBatchCount = info.TotalBatchCount,
                    MatchPredictionRunInitiatedUtc = now,
                    Status = ParallelMatchPredictionRunStatus.Running,
                    StatusDateUtc = now,
                    FinalisedTimeUtc = null,
                    IsSuccessful = null,
                };
                context.ParallelMatchPredictionRuns.Add(entity);

                // Pre-create one batch row per expected batch (in sequence order) so results can be recorded via UPDATE.
                var batches = new List<ParallelMatchPredictionBatch>(info.TotalBatchCount);
                for (var seq = 0; seq < info.TotalBatchCount; seq++)
                {
                    var matchPredictionBatch = new ParallelMatchPredictionBatch
                    {
                        Run = entity,
                        BatchSequenceNumber = seq,
                        BatchStatus = ParallelMatchPredictionBatchStatus.Requested,
                        BatchStatusDate = now,
                    };
                    batches.Add(matchPredictionBatch);
                    context.ParallelMatchPredictionBatches.Add(matchPredictionBatch);
                }

                await context.SaveChangesAsync();

                // Ids are populated by SaveChanges; batches is in ascending sequence order.
                var batchIdsBySequence = batches.Select(b => b.Id).ToList();
                return new CreateParallelMatchPredictionRunResult(entity.Id, batchIdsBySequence);
            }
        );
    }

    public async Task<bool> RecordBatchResult(int batchId, string resultLocation)
    {
        var now = DateTime.UtcNow;

        // Atomically update only if the batch has not yet received a result — this covers the normal Requested
        // state and a late result arriving for an Abandoned batch (replay). Prevents overwriting a duplicate
        // delivery; a row deleted by clean-up matches nothing and falls through to the not-found check below.
        var rowsUpdated = await context.ParallelMatchPredictionBatches
            .Where(b => b.Id == batchId
                     && (b.BatchStatus == ParallelMatchPredictionBatchStatus.Requested
                      || b.BatchStatus == ParallelMatchPredictionBatchStatus.Abandoned)
            )
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.BatchStatus, ParallelMatchPredictionBatchStatus.ResultsReceived)
                .SetProperty(b => b.BatchStatusDate, now)
                .SetProperty(b => b.ResultReceivedTimeUtc, now)
                .SetProperty(b => b.ResultLocation, resultLocation)
            );

        if (rowsUpdated == 1)
        {
            return true;
        }

        // Distinguish "already received" (idempotent duplicate) from "row not found" (data error).
        var exists = await context.ParallelMatchPredictionBatches
            .AnyAsync(b => b.Id == batchId);

        if (!exists)
        {
            throw new InvalidOperationException(
                $"No pre-created batch row found for BatchId={batchId}. "
              + "This indicates an invalid message or data corruption — a batch result was received for a batch that was never registered."
            );
        }

        // Row exists but result was already recorded — duplicate Service Bus delivery, treated as idempotent.
        return false;
    }

    public async Task<bool> RecordBatchFailure(int batchId, string failureMessage, string failureException)
    {
        var now = DateTime.UtcNow;

        // Atomically update only if the batch has not yet received a result — this covers the normal Requested
        // state and a late result arriving for an Abandoned batch (replay). Prevents overwriting a duplicate
        // delivery; a row deleted by clean-up matches nothing and falls through to the not-found check below.
        var rowsUpdated = await context.ParallelMatchPredictionBatches
            .Where(b => b.Id == batchId
                     && (b.BatchStatus == ParallelMatchPredictionBatchStatus.Requested
                      || b.BatchStatus == ParallelMatchPredictionBatchStatus.Abandoned)
            )
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.BatchStatus, ParallelMatchPredictionBatchStatus.Failed)
                .SetProperty(b => b.BatchStatusDate, now)
                .SetProperty(b => b.ResultReceivedTimeUtc, now)
                .SetProperty(b => b.FailureMessage, failureMessage)
                .SetProperty(b => b.FailureException, failureException)
            );

        if (rowsUpdated == 1)
        {
            return true;
        }

        var exists = await context.ParallelMatchPredictionBatches
            .AnyAsync(b => b.Id == batchId);

        if (!exists)
        {
            throw new InvalidOperationException(
                $"No pre-created batch row found for BatchId={batchId}. "
              + "This indicates an invalid message or data corruption — a batch failure was received for a batch that was never registered."
            );
        }

        // Row exists but was already recorded — duplicate Service Bus delivery, treated as idempotent.
        return false;
    }

    public async Task<IReadOnlyList<int>> GetRunIdsAwaitingFinalisationAndNotLeased()
    {
        // A run is ready to finalise when no other invocation has claimed it (FinalisationLeaseOwner IS NULL) and
        // every batch has a result (ResultsReceived or Failed). This also re-picks an Abandoned run once every
        // previously-missing batch's late result has arrived (no batch left Requested or Abandoned), so the run can
        // be replayed to Finalised. Normal Running runs never have Abandoned batches, so their behaviour is unchanged.
        return await context.ParallelMatchPredictionRuns
            .AsNoTracking()
            .Where(r => (r.Status == ParallelMatchPredictionRunStatus.Running
                      || r.Status == ParallelMatchPredictionRunStatus.Abandoned)
                     && r.FinalisationLeaseOwner == null
                     && !r.IsCleanedUp
                     && r.Batches.All(b => b.BatchStatus != ParallelMatchPredictionBatchStatus.Requested
                                        && b.BatchStatus != ParallelMatchPredictionBatchStatus.Abandoned
                        )
            )
            .Select(r => r.Id)
            .ToListAsync();
    }

    public async Task<bool> TryClaimFinalisationLease(int runId, Guid leaseOwner)
    {
        var rowsUpdated = await context.ParallelMatchPredictionRuns
            .Where(r => r.Id == runId
                     && (r.Status == ParallelMatchPredictionRunStatus.Running
                      || r.Status == ParallelMatchPredictionRunStatus.Abandoned)
                     && r.FinalisationLeaseOwner == null
                     && !r.IsCleanedUp
            )
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.FinalisationLeaseOwner, leaseOwner)
            );

        return rowsUpdated == 1;
    }

    public async Task<ParallelMatchPredictionRunResults> GetRunWithResults(int runId)
    {
        var run = await context.ParallelMatchPredictionRuns
            .AsNoTracking()
            .Include(r => r.Batches)
            .FirstOrDefaultAsync(r => r.Id == runId);

        if (run == null)
        {
            return null;
        }

        var batchResultLocations = run.Batches
            .Where(b => b.BatchStatus == ParallelMatchPredictionBatchStatus.ResultsReceived && b.ResultLocation != null)
            .Select(b => b.ResultLocation)
            .ToList();

        var failedBatches = run.Batches
            .Where(b => b.BatchStatus == ParallelMatchPredictionBatchStatus.Failed)
            .ToList();

        return new ParallelMatchPredictionRunResults
        {
            Run = run,
            BatchResultLocations = batchResultLocations,
            FailedBatches = failedBatches,
        };
    }

    public async Task MarkRunFinalised(int runId, DateTime finalisedTimeUtc)
    {
        await context.ParallelMatchPredictionRuns
            .Where(r => r.Id == runId
                     && (r.Status == ParallelMatchPredictionRunStatus.Running
                      || r.Status == ParallelMatchPredictionRunStatus.Abandoned)
            )
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.FinalisedTimeUtc, finalisedTimeUtc)
                .SetProperty(r => r.IsSuccessful, true)
                .SetProperty(r => r.Status, ParallelMatchPredictionRunStatus.Finalised)
                .SetProperty(r => r.StatusDateUtc, finalisedTimeUtc)
            );
    }

    public async Task MarkRunFailed(int runId, DateTime nowUtc)
    {
        await context.ParallelMatchPredictionRuns
            .Where(r => r.Id == runId
                     && (r.Status == ParallelMatchPredictionRunStatus.Running
                      || r.Status == ParallelMatchPredictionRunStatus.Abandoned)
            )
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.IsSuccessful, false)
                .SetProperty(r => r.Status, ParallelMatchPredictionRunStatus.FailedDuringBatchProcessing)
                .SetProperty(r => r.StatusDateUtc, nowUtc)
            );
    }

    public async Task MarkRunFailedDuringCompletion(int runId, DateTime nowUtc)
    {
        await context.ParallelMatchPredictionRuns
            .Where(r => r.Id == runId
                     && (r.Status == ParallelMatchPredictionRunStatus.Running
                      || r.Status == ParallelMatchPredictionRunStatus.Abandoned)
            )
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Status, ParallelMatchPredictionRunStatus.FailedDuringCompletion)
                .SetProperty(r => r.StatusDateUtc, nowUtc)
            );
    }

    public async Task<int> CleanupBatchesForRunsInitiatedBefore(DateTime cutoffUtc)
    {
        // Every run that has been in the database longer than the retention period and has not already
        // been cleaned up, regardless of status (Finalised, failed, or abandoned while still Running).
        // A Running run currently claimed for finalisation (FinalisationLeaseOwner set) is excluded:
        // deleting its batches mid-flight would let the completion pipeline read an empty batch set and
        // wrongly finalise the run as successful. Together with the single transaction below and the
        // !IsCleanedUp guard on TryClaimFinalisationLease this closes the race - a claimed run is skipped,
        // and an unclaimed run cleaned here can no longer be claimed afterwards.
        var runsToClean = context.ParallelMatchPredictionRuns
            .Where(r => !r.IsCleanedUp
                     && r.MatchPredictionRunInitiatedUtc < cutoffUtc
                     && (r.Status != ParallelMatchPredictionRunStatus.Running || r.FinalisationLeaseOwner == null)
            );

        // Use a transaction so the batch deletion and the parent-run flag update are atomic.
        var result = await context.ExecuteInTransactionAsync(async () =>
            {
                var deletedBatchCount = await context.ParallelMatchPredictionBatches
                    .Where(b => runsToClean.Any(r => r.Id == b.RunId))
                    .ExecuteDeleteAsync();

                // Mark the runs as cleaned up. Status is intentionally left unchanged so the run keeps its outcome.
                await runsToClean
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(r => r.IsCleanedUp, true)
                    );

                return deletedBatchCount;
            }
        );
        return result;
    }

    public async Task<IReadOnlyList<int>> GetRunIdsToAbandon(DateTime cutoffUtc)
    {
        // A run should be abandoned when it is still Running, was initiated before the cutoff, is not currently
        // being finalised (no lease), and has at least one batch that never returned a result (still Requested).
        return await context.ParallelMatchPredictionRuns
            .AsNoTracking()
            .Where(r => r.Status == ParallelMatchPredictionRunStatus.Running
                     && r.MatchPredictionRunInitiatedUtc < cutoffUtc
                     && r.FinalisationLeaseOwner == null
                     && r.Batches.Any(b => b.BatchStatus == ParallelMatchPredictionBatchStatus.Requested)
            )
            .Select(r => r.Id)
            .ToListAsync();
    }

    public Task<AbandonedRunHeader> TryMarkRunAsAbandoned(int runId, DateTime nowUtc)
    {
        return context.ExecuteInTransactionAsync(async () =>
            {
                // Compare-and-swap on status — only the first caller to flip Running -> Abandoned proceeds, so concurrent
                // timer ticks cannot both abandon (and double-notify) the same run. The lease check defers to any finaliser
                // that has already claimed the run: if a late batch result arrived in the window after GetRunIdsToAbandon
                // selected this run, a finaliser may have leased it for completion — abandoning it then would fire a
                // spurious failure notification/alert for a run that is about to finalise. The finaliser owns it instead.
                // FinalisationLeaseOwner is otherwise deliberately left null so a late-result
                var rowsUpdated = await context.ParallelMatchPredictionRuns
                    .Where(r => r.Id == runId
                             && r.Status == ParallelMatchPredictionRunStatus.Running
                             && r.FinalisationLeaseOwner == null
                    )
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(r => r.Status, ParallelMatchPredictionRunStatus.Abandoned)
                        .SetProperty(r => r.IsSuccessful, false)
                        .SetProperty(r => r.StatusDateUtc, nowUtc)
                    );

                if (rowsUpdated == 0)
                {
                    return null;
                }

                // Mark only the batches that never returned a result. Received/failed batches are kept as-is for research.
                await context.ParallelMatchPredictionBatches
                    .Where(b => b.RunId == runId && b.BatchStatus == ParallelMatchPredictionBatchStatus.Requested)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(b => b.BatchStatus, ParallelMatchPredictionBatchStatus.Abandoned)
                        .SetProperty(b => b.BatchStatusDate, nowUtc)
                    );

                return await context.ParallelMatchPredictionRuns
                    .AsNoTracking()
                    .Where(r => r.Id == runId)
                    .Select(r => new AbandonedRunHeader(r.SearchIdentifier, r.RepeatSearchIdentifier, r.IsRepeatSearch, r.TotalBatchCount))
                    .FirstAsync();
            }
        );
    }

    public async Task MarkRunAsDispatchFailed(int runId, string failureMessage, string failureException, DateTime nowUtc)
    {
        // An untruncated overlong message would fail the whole update with a SQL truncation error.
        var truncatedMessage = failureMessage?.Length > ParallelMatchPredictionBatch.FailureMessageMaxLength
            ? failureMessage[..ParallelMatchPredictionBatch.FailureMessageMaxLength]
            : failureMessage;

        await context.ExecuteInTransactionAsync(async () =>
            {
                // Compare-and-swap on status: only flag the run unsuccessful while it is still Running. Dispatch failure
                // happens moments after CreateRun, so the run is expected to be Running; the guard is defensive against a
                // late/retried call clobbering a run the finaliser has already taken to a terminal state. Status/StatusDateUtc
                // are deliberately not touched — the run must stay Running for the finaliser to pick it up.
                var runsUpdated = await context.ParallelMatchPredictionRuns
                    .Where(r => r.Id == runId
                             && r.Status == ParallelMatchPredictionRunStatus.Running
                    )
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(r => r.IsSuccessful, false)
                    );

                if (runsUpdated == 0)
                {
                    // The run is no longer Running — a finaliser has already taken it to a terminal state. Leave both the
                    // run and its batches untouched so a genuine outcome is never overwritten by a late dispatch-failure call.
                    return 0;
                }

                // Only flip batches that are still Requested. BatchPublish dispatches in successive physical Service Bus
                // batches, so a failure partway can leave earlier batches already dispatched and possibly already reported
                // by the Worker (ResultsReceived/Failed) — those terminal rows must be left intact rather than clobbered
                // back to a synthetic dispatch failure (which would drop their genuine result and mis-count the run).
                return await context.ParallelMatchPredictionBatches
                    .Where(b => b.RunId == runId && b.BatchStatus == ParallelMatchPredictionBatchStatus.Requested)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(b => b.BatchStatus, ParallelMatchPredictionBatchStatus.Failed)
                        .SetProperty(b => b.BatchStatusDate, nowUtc)
                        .SetProperty(b => b.FailureMessage, truncatedMessage)
                        .SetProperty(b => b.FailureException, failureException)
                    );
            }
        );
    }
}