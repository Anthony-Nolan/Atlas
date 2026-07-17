using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Sql;
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

    /// <summary>Batches whose status is <see cref="ParallelMatchPredictionBatchStatus.Failed"/>.</summary>
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
    /// Records a batch's successful result on its pre-created row: moves the batch to ResultsReceived from any
    /// not-yet-succeeded state (Requested, Abandoned, or Failed), clearing any recorded failure detail. Idempotent —
    /// if the batch already succeeded the call is a no-op returning <c>false</c>. Accepting Abandoned/Failed (not just
    /// Requested) is what lets a late result or a dead-letter replay recover the batch; an already-recorded success is
    /// never regressed by a later <see cref="RecordBatchFailure"/>.
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
    /// Returns the ids of unleased, not-cleaned-up runs the finalisation timer should (re-)process: a Running or
    /// replayed-Abandoned run whose every batch now has a result (success or failure); or a FailedDuringBatchProcessing
    /// run whose every batch has since recovered to ResultsReceived (a full dead-letter replay). The second case is
    /// deliberately stricter — a failed run has already reported its failure downstream, so it is only re-picked on a
    /// full recovery to success, never re-notified while any batch is still Failed.
    /// </summary>
    Task<IReadOnlyList<int>> GetRunIdsAwaitingFinalisationAndNotLeased();

    /// <summary>
    /// Attempts to atomically claim the run for finalisation by <paramref name="leaseOwner"/>: sets the lease only when
    /// the run is Running, Abandoned (replay) or FailedDuringBatchProcessing (re-finalisation after a full dead-letter
    /// recovery), currently unleased, and not cleaned up.
    /// </summary>
    /// <returns><c>true</c> if this call acquired the lease; <c>false</c> if another holds it or the run is not claimable.</returns>
    Task<bool> TryClaimFinalisationLease(int runId, Guid leaseOwner);

    /// <summary>
    /// Returns the run together with the blob locations of each successfully-received batch's result file.
    /// Returns <c>null</c> if the run does not exist.
    /// </summary>
    Task<ParallelMatchPredictionRunResults> GetRunWithResults(int runId);

    /// <summary>
    /// Flips a Running, Abandoned or FailedDuringBatchProcessing run to Finalised with IsSuccessful = true. The
    /// Abandoned and FailedDuringBatchProcessing cases are replays: once every batch has a result — late results, or a
    /// full dead-letter recovery of the failed batches — the finaliser re-picks the run and finalises it here.
    /// </summary>
    Task MarkRunFinalised(int runId, DateTime finalisedTimeUtc);

    /// <summary>
    /// Flips a Running or Abandoned run to FailedDuringBatchProcessing (IsSuccessful = false) when a batch failed in
    /// the Worker; the completion pipeline still runs. Non-terminal: the finalisation lease is released so that, if
    /// every failed batch is later recovered via dead-letter replay, the finaliser can re-claim and re-finalise the
    /// run to success.
    /// </summary>
    Task MarkRunFailed(int runId, DateTime nowUtc);

    /// <summary>
    /// Flips an actively-finalisable run (Running, Abandoned or FailedDuringBatchProcessing) to FailedDuringCompletion
    /// when the persistence pipeline throws. Terminal: the finalisation timer will not re-pick the run, and the lease
    /// is intentionally left in place.
    /// </summary>
    Task MarkRunFailedDuringCompletion(int runId, DateTime nowUtc);

    /// <summary>
    /// Deletes the batch rows of not-yet-cleaned-up runs initiated before <paramref name="cutoffUtc"/>, whatever their
    /// outcome, and flags those runs IsCleanedUp = true (Status is left unchanged so the run stays a historical record;
    /// parent run rows are retained). A run currently leased for finalisation is skipped so the in-flight completion
    /// pipeline never reads an empty batch set. Batch deletion and the flag update run in a single transaction.
    /// </summary>
    /// <returns>The number of batch rows deleted.</returns>
    Task<int> CleanupBatchesForRunsInitiatedBefore(DateTime cutoffUtc);

    /// <summary>
    /// Returns the ids of runs that should be abandoned: still Running, initiated before <paramref name="cutoffUtc"/>,
    /// not leased by a finaliser, and with at least one batch still Requested (a result never arrived).
    /// </summary>
    Task<IReadOnlyList<int>> GetRunIdsToAbandon(DateTime cutoffUtc);

    /// <summary>
    /// Atomically flips a single Running run to Abandoned (IsSuccessful = false) and marks its still-Requested batches
    /// Abandoned; batches that already have a result are left for research. The status compare-and-swap is the
    /// concurrency guard (only the first caller wins), and a run already leased by a finaliser is left to that finaliser
    /// rather than abandoned. The lease is left null so a late-result replay can still finalise the run.
    /// </summary>
    /// <returns>The run header if this call performed the transition; <c>null</c> if the run was no longer Running or already leased.</returns>
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

        var rowsUpdated = await context.ParallelMatchPredictionBatches
            .Where(b => b.Id == batchId
                     && (b.BatchStatus == ParallelMatchPredictionBatchStatus.Requested
                      || b.BatchStatus == ParallelMatchPredictionBatchStatus.Abandoned
                        || b.BatchStatus == ParallelMatchPredictionBatchStatus.Failed)
            )
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.BatchStatus, ParallelMatchPredictionBatchStatus.ResultsReceived)
                .SetProperty(b => b.BatchStatusDate, now)
                .SetProperty(b => b.ResultReceivedTimeUtc, now)
                .SetProperty(b => b.ResultLocation, resultLocation)
                .SetProperty(b => b.FailureMessage, (string)null)
                .SetProperty(b => b.FailureException, (string)null)
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
        return await context.ParallelMatchPredictionRuns
            .AsNoTracking()
            .Where(r => r.FinalisationLeaseOwner == null
                     && !r.IsCleanedUp
                     && (
                         ((r.Status == ParallelMatchPredictionRunStatus.Running
                           || r.Status == ParallelMatchPredictionRunStatus.Abandoned)
                          && r.Batches.All(b => b.BatchStatus != ParallelMatchPredictionBatchStatus.Requested
                                             && b.BatchStatus != ParallelMatchPredictionBatchStatus.Abandoned
                             ))
                         || (r.Status == ParallelMatchPredictionRunStatus.FailedDuringBatchProcessing
                             && r.Batches.All(b => b.BatchStatus == ParallelMatchPredictionBatchStatus.ResultsReceived))
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
                      || r.Status == ParallelMatchPredictionRunStatus.Abandoned
                      || r.Status == ParallelMatchPredictionRunStatus.FailedDuringBatchProcessing)
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
                      || r.Status == ParallelMatchPredictionRunStatus.Abandoned
                      || r.Status == ParallelMatchPredictionRunStatus.FailedDuringBatchProcessing)
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
                // Non-terminal: release the lease so a later full dead-letter recovery can re-claim and re-finalise the run.
                .SetProperty(r => r.FinalisationLeaseOwner, (Guid?)null)
            );
    }

    public async Task MarkRunFailedDuringCompletion(int runId, DateTime nowUtc)
    {
        await context.ParallelMatchPredictionRuns
            .Where(r => r.Id == runId
                     && (r.Status == ParallelMatchPredictionRunStatus.Running
                      || r.Status == ParallelMatchPredictionRunStatus.Abandoned
                      || r.Status == ParallelMatchPredictionRunStatus.FailedDuringBatchProcessing)
            )
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Status, ParallelMatchPredictionRunStatus.FailedDuringCompletion)
                .SetProperty(r => r.StatusDateUtc, nowUtc)
            );
    }

    public async Task<int> CleanupBatchesForRunsInitiatedBefore(DateTime cutoffUtc)
    {
        var runsToClean = context.ParallelMatchPredictionRuns
            .Where(r => !r.IsCleanedUp
                     && r.MatchPredictionRunInitiatedUtc < cutoffUtc
                     && (r.FinalisationLeaseOwner == null
                         || (r.Status != ParallelMatchPredictionRunStatus.Running
                          && r.Status != ParallelMatchPredictionRunStatus.Abandoned
                          && r.Status != ParallelMatchPredictionRunStatus.FailedDuringBatchProcessing))
            );

        var result = await context.ExecuteInTransactionAsync(async () =>
            {
                var deletedBatchCount = await context.ParallelMatchPredictionBatches
                    .Where(b => runsToClean.Any(r => r.Id == b.RunId))
                    .ExecuteDeleteAsync();

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
        var truncatedMessage = failureMessage?.Length > StringColumnLengths.LongText
            ? failureMessage[..StringColumnLengths.LongText]
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