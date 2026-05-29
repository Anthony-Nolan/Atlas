using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.Data.Context;
using Atlas.MatchPrediction.Data.Models;
using Microsoft.Data.SqlClient;
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
/// A run together with the merged donor → blob location map built from all of its batch rows.
/// </summary>
public class ParallelMatchPredictionRunResults
{
    public ParallelMatchPredictionRun Run { get; init; }

    public IReadOnlyDictionary<int, string> MergedResultLocations { get; init; }
}

public interface IParallelMatchPredictionRepository
{
    /// <summary>
    /// Creates the parent run record before any batches are dispatched. Returns the new run id.
    /// </summary>
    Task<int> CreateRun(CreateParallelMatchPredictionRunInfo info);

    /// <summary>
    /// Records a single batch result, keyed by <c>(runId, batchSequenceNumber)</c>.
    /// Idempotent: duplicate Service Bus deliveries are detected by the unique constraint and ignored.
    /// </summary>
    /// <returns><c>true</c> if a new row was inserted; <c>false</c> if this batch had already been recorded.</returns>
    Task<bool> RecordBatchResult(int runId, int batchSequenceNumber, IReadOnlyDictionary<int, string> resultLocations);

    /// <summary>
    /// Returns the ids of all runs that are ready to be finalised: they (a) have received
    /// <c>TotalBatchCount</c> batch rows, (b) have not yet been finalised, and (c) are not currently held
    /// by a live finalisation lease (i.e. no lease, or the lease has expired).
    /// Intended for the finalisation timer.
    /// </summary>
    Task<IReadOnlyList<int>> GetRunIdsAwaitingFinalisation(DateTime nowUtc);

    /// <summary>
    /// Returns the run together with the merged donor → blob location map built from its batch rows.
    /// Returns <c>null</c> if the run does not exist.
    /// </summary>
    Task<ParallelMatchPredictionRunResults> GetRunWithResults(int runId);

    /// <summary>
    /// Atomically claims a finalisation lease on the run for <paramref name="leaseOwner"/>, provided the run
    /// is not already finalised and is not currently held by another live lease. The lease is granted until
    /// <paramref name="nowUtc"/> + <paramref name="leaseDuration"/>.
    /// </summary>
    /// <returns><c>true</c> if the caller won the lease and should perform the persistence work; <c>false</c> otherwise.</returns>
    Task<bool> TryClaimRunForFinalisation(int runId, Guid leaseOwner, DateTime nowUtc, TimeSpan leaseDuration);

    /// <summary>
    /// Marks the run as fully finalised, but only if it is still not finalised and the caller
    /// (<paramref name="leaseOwner"/>) still holds the lease. Call this as the very last step, after all
    /// persistence has succeeded.
    /// </summary>
    /// <returns><c>true</c> if the run was marked finalised by this caller; <c>false</c> if it was already
    /// finalised or the lease had been taken over by another finaliser.</returns>
    Task<bool> MarkRunFinalised(int runId, Guid leaseOwner, DateTime finalisedTimeUtc);

    /// <summary>
    /// Deletes batch rows belonging to runs that finalised before <paramref name="cutoffUtc"/>.
    /// Parent run rows are intentionally retained.
    /// </summary>
    /// <returns>The number of batch rows deleted.</returns>
    Task<int> DeleteBatchesForRunsFinalisedBefore(DateTime cutoffUtc);
}

public class ParallelMatchPredictionRepository : IParallelMatchPredictionRepository
{
    // SQL Server error numbers raised when an INSERT violates a uniqueness guarantee.
    // 2627 = violation of a UNIQUE constraint / PRIMARY KEY; 2601 = duplicate key row in a UNIQUE index.
    // https://learn.microsoft.com/en-us/sql/relational-databases/errors-events/database-engine-events-and-errors-2000-to-2999
    private const int SqlServerUniqueConstraintViolation = 2627;
    private const int SqlServerDuplicateKeyInUniqueIndex = 2601;

    private readonly MatchPredictionContext context;

    public ParallelMatchPredictionRepository(MatchPredictionContext context)
    {
        this.context = context;
    }

    public async Task<int> CreateRun(CreateParallelMatchPredictionRunInfo info)
    {
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
            FinalisedTimeUtc = null,
        };
        context.ParallelMatchPredictionRuns.Add(entity);
        await context.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<bool> RecordBatchResult(int runId, int batchSequenceNumber, IReadOnlyDictionary<int, string> resultLocations)
    {
        var batch = new ParallelMatchPredictionBatch
        {
            RunId = runId,
            BatchSequenceNumber = batchSequenceNumber,
            ReceivedTimeUtc = DateTime.UtcNow,
            ResultLocationsJson = JsonSerializer.Serialize(resultLocations),
        };

        context.ParallelMatchPredictionBatches.Add(batch);

        try
        {
            await context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException ex)
        {
            // Detach the failed (never-inserted) entity so subsequent operations on this context are unaffected.
            context.Entry(batch).State = EntityState.Detached;

            // Fast, deterministic path for SQL Server (the production provider): a unique-constraint error
            // number unambiguously identifies the expected duplicate Service Bus delivery, with no extra query.
            if (IsSqlServerDuplicateKeyViolation(ex))
            {
                return false;
            }

            throw;
        }
    }

    /// <summary>
    /// Determines whether the given exception was caused by a SQL Server uniqueness violation
    /// by inspecting the underlying <see cref="SqlException"/> error number. This is deterministic
    /// and avoids treating unrelated <see cref="DbUpdateException"/>s (FK violations, timeouts,
    /// deadlocks, etc.) as duplicates.
    /// </summary>
    private static bool IsSqlServerDuplicateKeyViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: SqlServerUniqueConstraintViolation or SqlServerDuplicateKeyInUniqueIndex };

    public async Task<IReadOnlyList<int>> GetRunIdsAwaitingFinalisation(DateTime nowUtc)
    {
        // A run is ready to finalise when all its batch rows are present, it has not been finalised, and it
        // is not currently held by a live finalisation lease (no lease, or an expired one).
        return await context.ParallelMatchPredictionRuns
            .AsNoTracking()
            .Where(r => r.FinalisedTimeUtc == null
                     && (r.FinalisationLeaseExpiresUtc == null || r.FinalisationLeaseExpiresUtc < nowUtc)
                     && r.Batches.Count == r.TotalBatchCount
            )
            .Select(r => r.Id)
            .ToListAsync();
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
        foreach (var batch in run.Batches)
        {
            var currentBatchResultLocations = JsonSerializer.Deserialize<Dictionary<int, string>>(batch.ResultLocationsJson)
                                           ?? new Dictionary<int, string>();
            mergedResultLocations = mergedResultLocations.Merge(currentBatchResultLocations);
        }

        return new ParallelMatchPredictionRunResults
        {
            Run = run,
            MergedResultLocations = mergedResultLocations
        };
    }

    public async Task<bool> TryClaimRunForFinalisation(int runId, Guid leaseOwner, DateTime nowUtc, TimeSpan leaseDuration)
    {
        // Single conditional UPDATE acts as the atomic claim: only one finaliser can transition the run from
        // "unclaimed / lease expired" to "leased by me". The returned row count tells us whether we won.
        var leaseExpiresUtc = nowUtc.Add(leaseDuration);
        var rowsUpdated = await context.ParallelMatchPredictionRuns
            .Where(r => r.Id == runId
                     && r.FinalisedTimeUtc == null
                     && (r.FinalisationLeaseExpiresUtc == null || r.FinalisationLeaseExpiresUtc < nowUtc)
            )
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.FinalisationLeaseOwner, leaseOwner)
                .SetProperty(r => r.FinalisationLeaseExpiresUtc, leaseExpiresUtc)
            );
        return rowsUpdated == 1;
    }

    public async Task<bool> MarkRunFinalised(int runId, Guid leaseOwner, DateTime finalisedTimeUtc)
    {
        // Only the current leaseholder may finalise. If a slow finaliser's lease expired and was re-claimed
        // by another instance, the owner check prevents it from marking the run done out from under the new owner.
        var rowsUpdated = await context.ParallelMatchPredictionRuns
            .Where(r => r.Id == runId
                     && r.FinalisedTimeUtc == null
                     && r.FinalisationLeaseOwner == leaseOwner
            )
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.FinalisedTimeUtc, finalisedTimeUtc));
        return rowsUpdated == 1;
    }

    public async Task<int> DeleteBatchesForRunsFinalisedBefore(DateTime cutoffUtc)
    {
        return await context.ParallelMatchPredictionBatches
            .Where(b => b.Run.FinalisedTimeUtc != null && b.Run.FinalisedTimeUtc < cutoffUtc)
            .ExecuteDeleteAsync();
    }
}