using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
/// A run together with the merged donor → blob location map built from all of its received batch rows.
/// </summary>
public class ParallelMatchPredictionRunResults
{
    public ParallelMatchPredictionRun Run { get; init; }

    public IReadOnlyDictionary<int, string> MergedResultLocations { get; init; }

    /// <summary>Batches whose <see cref="ParallelMatchPredictionBatch.BatchStatus"/> is <see cref="ParallelMatchPredictionBatchStatus.Failed"/>.</summary>
    public IReadOnlyList<ParallelMatchPredictionBatch> FailedBatches { get; init; }
}

public interface IParallelMatchPredictionRepository
{
    /// <summary>
    /// Creates the parent run record (with status <see cref="ParallelMatchPredictionRunStatus.Running"/>) and
    /// pre-creates one <see cref="ParallelMatchPredictionBatch"/> row per expected batch
    /// (<c>BatchSequenceNumber</c> 0 … <c>TotalBatchCount − 1</c>) before any batch messages are dispatched.
    /// Returns the new run id.
    /// </summary>
    Task<int> CreateRun(CreateParallelMatchPredictionRunInfo info);

    /// <summary>
    /// Records a single successful batch result by updating the pre-created batch row keyed by
    /// <c>(runId, batchSequenceNumber)</c>. Idempotent: if the result was already recorded the update
    /// is a no-op and <c>false</c> is returned.
    /// </summary>
    /// <returns>
    /// <c>true</c> if this is the first time the result was recorded; <c>false</c> if it was a duplicate.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no pre-created batch row exists for the given key, which indicates an invalid message or data corruption.
    /// </exception>
    Task<bool> RecordBatchResult(int runId, int batchSequenceNumber, IReadOnlyDictionary<int, string> resultLocations);

    /// <summary>
    /// Records a batch failure by updating the pre-created batch row keyed by
    /// <c>(runId, batchSequenceNumber)</c>. Idempotent: if the batch was already recorded the update
    /// is a no-op and <c>false</c> is returned.
    /// </summary>
    /// <returns>
    /// <c>true</c> if this is the first time the failure was recorded; <c>false</c> if it was a duplicate.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no pre-created batch row exists for the given key.
    /// </exception>
    Task<bool> RecordBatchFailure(int runId, int batchSequenceNumber, string failureMessage, string failureException);

    /// <summary>
    /// Returns the ids of all <see cref="ParallelMatchPredictionRunStatus.Running"/> runs whose every batch
    /// has a <see cref="ParallelMatchPredictionBatch.BatchStatus"/> other than
    /// <see cref="ParallelMatchPredictionBatchStatus.Requested"/> (i.e. every batch has a result, success or failure)
    /// <em>and</em> that have not yet been claimed by another invocation
    /// (i.e. <see cref="ParallelMatchPredictionRun.FinalisationLeaseOwner"/> is <c>null</c>).
    /// Intended for the finalisation timer.
    /// </summary>
    Task<IReadOnlyList<int>> GetRunIdsAwaitingFinalisationAndNotLeased();

    /// <summary>
    /// Attempts to atomically claim the given run for finalisation by this invocation.
    /// Sets <see cref="ParallelMatchPredictionRun.FinalisationLeaseOwner"/> to <paramref name="leaseOwner"/>
    /// only when the run has status <see cref="ParallelMatchPredictionRunStatus.Running"/> and its
    /// <see cref="ParallelMatchPredictionRun.FinalisationLeaseOwner"/> is currently <c>null</c>.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the lease was successfully acquired by this call; <c>false</c> if another invocation
    /// already holds the lease or the run is no longer in the <c>Running</c> state.
    /// </returns>
    Task<bool> TryClaimFinalisationLease(int runId, Guid leaseOwner);

    /// <summary>
    /// Returns the run together with the merged donor → blob location map built from its received batch rows.
    /// Returns <c>null</c> if the run does not exist.
    /// </summary>
    Task<ParallelMatchPredictionRunResults> GetRunWithResults(int runId);

    /// <summary>
    /// Marks the run as <see cref="ParallelMatchPredictionRunStatus.Finalised"/> and sets
    /// <see cref="ParallelMatchPredictionRun.IsSuccessful"/> to <c>true</c>, provided it still has
    /// status <see cref="ParallelMatchPredictionRunStatus.Running"/>. Call this as the very last step,
    /// after all persistence has succeeded.
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
    /// <c>MatchPredictionRunInitiatedUtc</c> is before <paramref name="cutoffUtc"/> — i.e. every run that has
    /// been in the database longer than the retention period, regardless of status (including abandoned
    /// <see cref="ParallelMatchPredictionRunStatus.Running"/> runs and failed runs). Marks those runs as
    /// cleaned up by setting <see cref="ParallelMatchPredictionRun.IsCleanedUp"/> to <c>true</c>; the run's
    /// <see cref="ParallelMatchPredictionRun.Status"/> is left unchanged so it remains a historical record.
    /// The whole operation is wrapped in a transaction so that the flag update and batch deletion are atomic.
    /// Parent run rows are intentionally retained.
    /// </summary>
    /// <returns>The number of batch rows deleted.</returns>
    Task<int> CleanupBatchesForRunsCreatedBefore(DateTime cutoffUtc);
}

public class ParallelMatchPredictionRepository : IParallelMatchPredictionRepository
{
    private readonly MatchPredictionContext context;

    public ParallelMatchPredictionRepository(MatchPredictionContext context)
    {
        this.context = context;
    }

    public async Task<int> CreateRun(CreateParallelMatchPredictionRunInfo info)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
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

            // Pre-create one batch row per expected batch so that results can be recorded via UPDATE rather than INSERT.
            for (var seq = 0; seq < info.TotalBatchCount; seq++)
            {
                var matchPredictionBatch = new ParallelMatchPredictionBatch
                {
                    Run = entity,
                    BatchSequenceNumber = seq,
                    BatchStatus = ParallelMatchPredictionBatchStatus.Requested,
                };
                context.ParallelMatchPredictionBatches.Add(matchPredictionBatch);
            }

            await context.SaveChangesAsync();

            await transaction.CommitAsync();
            return entity.Id;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> RecordBatchResult(int runId, int batchSequenceNumber, IReadOnlyDictionary<int, string> resultLocations)
    {
        var now = DateTime.UtcNow;
        var serializedLocations = JsonSerializer.Serialize(resultLocations);

        // Atomically update only if still Requested — prevents overwriting with a duplicate delivery.
        var rowsUpdated = await context.ParallelMatchPredictionBatches
            .Where(b => b.RunId == runId
                     && b.BatchSequenceNumber == batchSequenceNumber
                     && b.BatchStatus == ParallelMatchPredictionBatchStatus.Requested
            )
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.BatchStatus, ParallelMatchPredictionBatchStatus.ResultsReceived)
                .SetProperty(b => b.ResultReceivedTimeUtc, now)
                .SetProperty(b => b.ResultLocationJson, serializedLocations)
            );

        if (rowsUpdated == 1)
        {
            return true;
        }

        // Distinguish "already received" (idempotent duplicate) from "row not found" (data error).
        var exists = await context.ParallelMatchPredictionBatches
            .AnyAsync(b => b.RunId == runId && b.BatchSequenceNumber == batchSequenceNumber);

        if (!exists)
        {
            throw new InvalidOperationException(
                $"No pre-created batch row found for RunId={runId}, BatchSequenceNumber={batchSequenceNumber}. "
              + "This indicates an invalid message or data corruption — a batch result was received for a run/batch that was never registered."
            );
        }

        // Row exists but result was already recorded — duplicate Service Bus delivery, treated as idempotent.
        return false;
    }

    public async Task<bool> RecordBatchFailure(int runId, int batchSequenceNumber, string failureMessage, string failureException)
    {
        var now = DateTime.UtcNow;

        // Atomically update only if still Requested — prevents overwriting with a duplicate delivery.
        var rowsUpdated = await context.ParallelMatchPredictionBatches
            .Where(b => b.RunId == runId
                     && b.BatchSequenceNumber == batchSequenceNumber
                     && b.BatchStatus == ParallelMatchPredictionBatchStatus.Requested
            )
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.BatchStatus, ParallelMatchPredictionBatchStatus.Failed)
                .SetProperty(b => b.ResultReceivedTimeUtc, now)
                .SetProperty(b => b.FailureMessage, failureMessage)
                .SetProperty(b => b.FailureException, failureException)
            );

        if (rowsUpdated == 1)
        {
            return true;
        }

        var exists = await context.ParallelMatchPredictionBatches
            .AnyAsync(b => b.RunId == runId && b.BatchSequenceNumber == batchSequenceNumber);

        if (!exists)
        {
            throw new InvalidOperationException(
                $"No pre-created batch row found for RunId={runId}, BatchSequenceNumber={batchSequenceNumber}. "
              + "This indicates an invalid message or data corruption — a batch failure was received for a run/batch that was never registered."
            );
        }

        // Row exists but was already recorded — duplicate Service Bus delivery, treated as idempotent.
        return false;
    }

    public async Task<IReadOnlyList<int>> GetRunIdsAwaitingFinalisationAndNotLeased()
    {
        // A run is ready to finalise when it is Running, every batch has moved past the Requested state
        // (i.e. every batch is either ResultsReceived or Failed), and no other invocation has already
        // claimed it (FinalisationLeaseOwner IS NULL).
        return await context.ParallelMatchPredictionRuns
            .AsNoTracking()
            .Where(r => r.Status == ParallelMatchPredictionRunStatus.Running
                     && r.FinalisationLeaseOwner == null
                     && !r.IsCleanedUp
                     && r.Batches.All(b => b.BatchStatus != ParallelMatchPredictionBatchStatus.Requested)
            )
            .Select(r => r.Id)
            .ToListAsync();
    }

    public async Task<bool> TryClaimFinalisationLease(int runId, Guid leaseOwner)
    {
        var rowsUpdated = await context.ParallelMatchPredictionRuns
            .Where(r => r.Id == runId
                     && r.Status == ParallelMatchPredictionRunStatus.Running
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

        var mergedResultLocations = new Dictionary<int, string>();
        foreach (var batch in run.Batches.Where(b => b.BatchStatus == ParallelMatchPredictionBatchStatus.ResultsReceived))
        {
            var currentBatchResultLocations = JsonSerializer.Deserialize<Dictionary<int, string>>(batch.ResultLocationJson)
                                           ?? new Dictionary<int, string>();
            mergedResultLocations = mergedResultLocations.Merge(currentBatchResultLocations);
        }

        var failedBatches = run.Batches
            .Where(b => b.BatchStatus == ParallelMatchPredictionBatchStatus.Failed)
            .ToList();

        return new ParallelMatchPredictionRunResults
        {
            Run = run,
            MergedResultLocations = mergedResultLocations,
            FailedBatches = failedBatches,
        };
    }

    public async Task MarkRunFinalised(int runId, DateTime finalisedTimeUtc)
    {
        await context.ParallelMatchPredictionRuns
            .Where(r => r.Id == runId && r.Status == ParallelMatchPredictionRunStatus.Running)
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
            .Where(r => r.Id == runId && r.Status == ParallelMatchPredictionRunStatus.Running)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.IsSuccessful, false)
                .SetProperty(r => r.Status, ParallelMatchPredictionRunStatus.FailedDuringBatchProcessing)
                .SetProperty(r => r.StatusDateUtc, nowUtc)
            );
    }

    public async Task MarkRunFailedDuringCompletion(int runId, DateTime nowUtc)
    {
        await context.ParallelMatchPredictionRuns
            .Where(r => r.Id == runId && r.Status == ParallelMatchPredictionRunStatus.Running)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Status, ParallelMatchPredictionRunStatus.FailedDuringCompletion)
                .SetProperty(r => r.StatusDateUtc, nowUtc)
            );
    }

    public async Task<int> CleanupBatchesForRunsCreatedBefore(DateTime cutoffUtc)
    {
        // Every run that has been in the database longer than the retention period and has not already
        // been cleaned up, regardless of status (Finalised, failed, or abandoned while still Running).
        var runIds = await context.ParallelMatchPredictionRuns
            .AsNoTracking()
            .Where(r => !r.IsCleanedUp
                     && r.MatchPredictionRunInitiatedUtc < cutoffUtc
            )
            .Select(r => r.Id)
            .ToListAsync();

        if (runIds.Count == 0)
        {
            return 0;
        }

        // Use a transaction so the batch deletion and the parent-run flag update are atomic.
        await using var transaction = await context.Database.BeginTransactionAsync();

        var deletedBatchCount = await context.ParallelMatchPredictionBatches
            .Where(b => runIds.Contains(b.RunId))
            .ExecuteDeleteAsync();

        // Mark the runs as cleaned up. Status is intentionally left unchanged so the run keeps its outcome.
        await context.ParallelMatchPredictionRuns
            .Where(r => runIds.Contains(r.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.IsCleanedUp, true)
            );

        await transaction.CommitAsync();
        return deletedBatchCount;
    }
}