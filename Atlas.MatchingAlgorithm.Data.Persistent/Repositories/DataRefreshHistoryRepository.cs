using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Data.Persistent.Context;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using EnumStringValues;
using Microsoft.EntityFrameworkCore;

namespace Atlas.MatchingAlgorithm.Data.Persistent.Repositories
{
    public interface IDataRefreshHistoryRepository
    {
        /// <returns>The transient database for which the refresh job was most recently completed</returns>
        TransientDatabase? GetActiveDatabase();

        /// <returns>The HLA nomenclature version used in the most recently completed refresh job</returns>
        string GetActiveHlaNomenclatureVersion();

        /// <returns>
        /// The id, database and HLA nomenclature version of the most recently completed refresh job, all read from the
        /// same record; or null if no refresh job has completed yet.
        /// </returns>
        ActiveDataRefreshRecord GetActiveRecord();

        IEnumerable<DataRefreshRecord> GetIncompleteRefreshJobs();

        /// <returns>The transient database of each open refresh record, by record id.</returns>
        /// <remarks>
        /// Reads only the id and the database of each record. So a process whose model has a column that the database does
        /// not have yet - a new release that runs before its migration - can still call it.
        /// </remarks>
        Task<IReadOnlyDictionary<int, TransientDatabase>> GetIncompleteRefreshJobDatabases();

        Task<int> Create(DataRefreshRecord dataRefreshRecord);

        Task<DataRefreshRecord> GetRecord(int dataRefreshRecordId);
        Task UpdateRunAttemptDetails(int recordId);
        Task UpdateExecutionDetails(int recordId, string wmdaHlaNomenclatureVersion, DateTime? finishTimeUtc = null);
        Task UpdateSuccessFlag(int recordId, bool wasSuccess);
        Task UpdateLastSafelyProcessedDonor(int recordId, int donorId);
        Task MarkStageAsComplete(DataRefreshRecord record, DataRefreshStage stage);

        /// <summary>
        /// Records the HLA Metadata Dictionary snapshot the run created, and marks the
        /// <see cref="DataRefreshStage.MetadataDictionaryRefresh"/> stage as complete, in one write: a record never shows
        /// the stage as complete without the snapshot it produced.
        /// </summary>
        Task MarkHlaMetadataDictionaryRefreshAsComplete(DataRefreshRecord record, DateTime snapshotUtc);

        /// <summary>
        /// Attempts to take the run-level lease on a record, so that only one invocation processes it at a time.
        /// The claim succeeds if the record is still open, is not paused for a manual decision, and is either unleased,
        /// leased to <paramref name="owner"/> already, or held by an owner whose lease has expired.
        /// </summary>
        /// <remarks>
        /// Re-claiming for the current <paramref name="owner"/> is deliberately permitted, so that a message redelivered
        /// to the invocation that already holds the lease is harmless.
        ///
        /// A paused record (<see cref="DataRefreshRecord.AwaitingManualDecisionSinceUtc"/>) is refused, so a redelivered or
        /// replayed request does not resume it. Only a manual decision ends the pause.
        /// </remarks>
        /// <returns>
        /// True if this owner now holds the lease. False if another invocation holds it, the record is already finished, or
        /// the record is paused.
        /// </returns>
        Task<bool> TryClaimRefreshLease(int recordId, Guid owner, DateTime nowUtc, TimeSpan ttl);

        /// <summary>
        /// Extends the lease expiry, provided <paramref name="owner"/> still holds it.
        /// </summary>
        /// <returns>False if the lease has been taken over by another invocation, i.e. this owner has been fenced.</returns>
        Task<bool> TryRenewRefreshLease(int recordId, Guid owner, DateTime expiry);

        /// <summary>
        /// Gives up the lease, provided <paramref name="owner"/> still holds it, so the next invocation need not wait
        /// out the remaining lease duration.
        /// </summary>
        /// <returns>False if this owner had already been fenced, and so released nothing.</returns>
        Task<bool> ReleaseRefreshLease(int recordId, Guid owner);

        /// <summary>
        /// Finds refresh records that are still open but that nothing is working on, i.e. that have stalled and will
        /// never complete unless they are re-requested.
        /// </summary>
        /// <remarks>
        /// The lease clause is deliberately phrased the way <see cref="TryClaimRefreshLease"/> decides claimability,
        /// rather than in terms of the expiry alone. That call reads a set owner with no expiry as still held, since
        /// SQL makes the expiry comparison UNKNOWN; keying off the expiry here instead would re-request such a record
        /// on every sweep, forever, while no invocation could ever claim it.
        ///
        /// The idle check is the other non-obvious clause. An absent <see cref="DataRefreshRecord.LeaseOwner"/> is not by
        /// itself evidence of a stall: a record created moments ago, whose request message is still in flight, has yet
        /// to be leased, and so has one whose run has just failed and released its lease while Service Bus redelivers
        /// the request. Both recover unaided. Requiring the record to have been idle for the whole grace period as well
        /// excludes them, and catches the one stall no lease can reveal - a request message that was never delivered.
        ///
        /// A paused record is not stalled: it waits for a manual decision, and no invocation can claim it. A record with a
        /// pending decision is not paused, so it is found here when the request of the decision was not published.
        /// </remarks>
        /// <param name="graceCutoffUtc">The record must have been idle since before this time to count as stalled.</param>
        Task<IReadOnlyCollection<int>> GetStalledRefreshRecordIds(DateTime graceCutoffUtc);

        /// <returns>The record with this id, or null when there is none. Not tracked, so it always shows the stored values.</returns>
        Task<DataRefreshRecord> FindRecord(int recordId);

        /// <returns>The open records that are paused for a manual decision. Not tracked.</returns>
        Task<IReadOnlyCollection<DataRefreshRecord>> GetPausedRefreshRecords();

        /// <summary>
        /// Pauses the record for a manual decision, and releases the lease, in one update. Only while
        /// <paramref name="owner"/> holds the lease, and the record is open.
        /// </summary>
        /// <remarks>
        /// One update, so a paused record never has a lease, and an invocation that stops before the update leaves a record
        /// that is open and not paused, which the watchdog resumes.
        /// </remarks>
        /// <returns>False when this owner does not hold the lease, or the record is closed: nothing changed.</returns>
        Task<bool> TryPauseAndReleaseLease(int recordId, Guid owner, DateTime pausedSinceUtc);

        /// <summary>
        /// Takes a manual decision for a paused record, in one compare-and-swap update: only while the record is open and
        /// paused. The update ends the pause, and appends <paramref name="supportComment"/> to
        /// <see cref="DataRefreshRecord.SupportComments"/> as a new line.
        /// <see cref="DataRefreshManualDecision.Complete"/> and <see cref="DataRefreshManualDecision.RetryFailedBatches"/>
        /// become the <see cref="DataRefreshRecord.PendingManualDecision"/>.
        /// <see cref="DataRefreshManualDecision.Fail"/> closes the record as failed.
        /// </summary>
        /// <remarks>
        /// No lease check: a paused record never has a lease, and no invocation can claim it while it is paused.
        /// </remarks>
        /// <returns>False when the record is not paused, or is closed: nothing changed.</returns>
        Task<bool> TryTakeManualDecision(int recordId, DataRefreshManualDecision decision, DateTime nowUtc, string supportComment);

        /// <summary>
        /// Applies a pending <see cref="DataRefreshManualDecision.Complete"/> decision: marks the
        /// <see cref="DataRefreshStage.DonorGenotypePrecomputation"/> stage as complete, and clears the decision, in one write.
        /// </summary>
        Task ApplyManualCompleteDecision(DataRefreshRecord record);

        /// <summary>Clears the <see cref="DataRefreshRecord.PendingManualDecision"/> of the record.</summary>
        Task ClearPendingManualDecision(DataRefreshRecord record);

        /// <summary>
        /// Ends the pause, and clears the pending decision, of the record. For a record that is closed as failed: by the manual
        /// cleanup, or after a refresh that failed.
        /// </summary>
        Task ClearManualDecisionState(int recordId);
    }

    public class DataRefreshHistoryRepository : IDataRefreshHistoryRepository
    {
        protected readonly SearchAlgorithmPersistentContext Context;

        public DataRefreshHistoryRepository(SearchAlgorithmPersistentContext context)
        {
            Context = context;
        }

        public TransientDatabase? GetActiveDatabase()
        {
            var lastCompletedRecord = GetLastSuccessfulRecord();

            return lastCompletedRecord?.Database.ParseToEnum<TransientDatabase>();
        }

        public string GetActiveHlaNomenclatureVersion()
        {
            var lastCompletedRecord = GetLastSuccessfulRecord();
            return lastCompletedRecord?.HlaNomenclatureVersion;
        }

        public ActiveDataRefreshRecord GetActiveRecord()
        {
            var lastCompletedRecord = GetLastSuccessfulRecord();

            return lastCompletedRecord == null
                ? null
                : new ActiveDataRefreshRecord(
                    lastCompletedRecord.Id,
                    lastCompletedRecord.Database.ParseToEnum<TransientDatabase>(),
                    lastCompletedRecord.HlaNomenclatureVersion);
        }

        public IEnumerable<DataRefreshRecord> GetIncompleteRefreshJobs()
        {
            return Context.DataRefreshRecords.Where(r => r.RefreshEndUtc == null);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyDictionary<int, TransientDatabase>> GetIncompleteRefreshJobDatabases()
        {
            var records = await Context.DataRefreshRecords
                .Where(r => r.RefreshEndUtc == null)
                .Select(r => new { r.Id, r.Database })
                .ToListAsync();

            return records.ToDictionary(r => r.Id, r => r.Database.ParseToEnum<TransientDatabase>());
        }

        public async Task<int> Create(DataRefreshRecord dataRefreshRecord)
        {
            // ReSharper disable once MethodHasAsyncOverload
            Context.DataRefreshRecords.Add(dataRefreshRecord);
            await Context.SaveChangesAsync();
            return dataRefreshRecord.Id;
        }

        public async Task UpdateRunAttemptDetails(int recordId)
        {
            var record = await GetRecord(recordId);
            record.RefreshLastContinuedUtc = DateTime.UtcNow;
            record.RefreshAttemptedCount++;
            await Context.SaveChangesAsync();
        }

        public async Task UpdateExecutionDetails(int recordId, string wmdaHlaNomenclatureVersion, DateTime? finishTimeUtc)
        {
            var record = await GetRecord(recordId);
            record.HlaNomenclatureVersion = wmdaHlaNomenclatureVersion ?? record.HlaNomenclatureVersion; // Don't wipe the HLA version if we already recorded it.
            record.RefreshEndUtc = finishTimeUtc;
            await Context.SaveChangesAsync();
        }

        public async Task UpdateSuccessFlag(int recordId, bool wasSuccess)
        {
            var record = await GetRecord(recordId);
            record.WasSuccessful = wasSuccess;
            await Context.SaveChangesAsync();
        }

        public async Task UpdateLastSafelyProcessedDonor(int recordId, int donorId)
        {
            var record = await GetRecord(recordId);
            record.LastSafelyProcessedDonor = donorId;
            await Context.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task MarkStageAsComplete(DataRefreshRecord record, DataRefreshStage stage)
        {
            record.SetStageCompletionTime(stage, DateTime.UtcNow);
            await Context.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task MarkHlaMetadataDictionaryRefreshAsComplete(DataRefreshRecord record, DateTime snapshotUtc)
        {
            record.HlaMetadataDictionarySnapshotUtc = snapshotUtc;
            record.SetStageCompletionTime(DataRefreshStage.MetadataDictionaryRefresh, DateTime.UtcNow);
            await Context.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task<bool> TryClaimRefreshLease(int recordId, Guid owner, DateTime nowUtc, TimeSpan ttl)
        {
            var expiry = nowUtc + ttl;

            var rowsUpdated = await Context.DataRefreshRecords
                .Where(r => r.Id == recordId
                            && r.RefreshEndUtc == null
                            && r.AwaitingManualDecisionSinceUtc == null
                            && (r.LeaseOwner == null || r.LeaseOwner == owner || r.LeaseExpiresUtc < nowUtc))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.LeaseOwner, owner)
                    .SetProperty(r => r.LeaseExpiresUtc, expiry));

            return rowsUpdated == 1;
        }

        /// <inheritdoc />
        public async Task<bool> TryRenewRefreshLease(int recordId, Guid owner, DateTime expiry)
        {
            var rowsUpdated = await Context.DataRefreshRecords
                .Where(r => r.Id == recordId && r.LeaseOwner == owner)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.LeaseExpiresUtc, expiry));

            return rowsUpdated == 1;
        }

        /// <inheritdoc />
        public async Task<bool> ReleaseRefreshLease(int recordId, Guid owner)
        {
            var rowsUpdated = await Context.DataRefreshRecords
                .Where(r => r.Id == recordId && r.LeaseOwner == owner)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.LeaseOwner, (Guid?) null)
                    .SetProperty(r => r.LeaseExpiresUtc, (DateTime?) null));

            return rowsUpdated == 1;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<int>> GetStalledRefreshRecordIds(DateTime graceCutoffUtc)
        {
            return await Context.DataRefreshRecords
                .Where(r => r.RefreshEndUtc == null
                            && r.AwaitingManualDecisionSinceUtc == null
                            && (r.LeaseOwner == null || r.LeaseExpiresUtc < graceCutoffUtc)
                            && (r.RefreshLastContinuedUtc ?? r.RefreshRequestedUtc) < graceCutoffUtc)
                .Select(r => r.Id)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<DataRefreshRecord> FindRecord(int recordId)
        {
            return await Context.DataRefreshRecords.AsNoTracking().SingleOrDefaultAsync(r => r.Id == recordId);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<DataRefreshRecord>> GetPausedRefreshRecords()
        {
            return await Context.DataRefreshRecords
                .AsNoTracking()
                .Where(r => r.RefreshEndUtc == null && r.AwaitingManualDecisionSinceUtc != null)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<bool> TryPauseAndReleaseLease(int recordId, Guid owner, DateTime pausedSinceUtc)
        {
            var rowsUpdated = await Context.DataRefreshRecords
                .Where(r => r.Id == recordId && r.LeaseOwner == owner && r.RefreshEndUtc == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.AwaitingManualDecisionSinceUtc, pausedSinceUtc)
                    .SetProperty(r => r.LeaseOwner, (Guid?) null)
                    .SetProperty(r => r.LeaseExpiresUtc, (DateTime?) null));

            return rowsUpdated == 1;
        }

        /// <inheritdoc />
        public async Task<bool> TryTakeManualDecision(int recordId, DataRefreshManualDecision decision, DateTime nowUtc, string supportComment)
        {
            var newLineAndComment = Environment.NewLine + supportComment;

            var rowsUpdated = await Context.DataRefreshRecords
                .Where(r => r.Id == recordId && r.RefreshEndUtc == null && r.AwaitingManualDecisionSinceUtc != null)
                .ExecuteUpdateAsync(s =>
                {
                    s.SetProperty(r => r.AwaitingManualDecisionSinceUtc, (DateTime?) null);
                    s.SetProperty(
                        r => r.SupportComments,
                        r => r.SupportComments == null || r.SupportComments == "" ? supportComment : r.SupportComments + newLineAndComment);

                    if (decision == DataRefreshManualDecision.Fail)
                    {
                        s.SetProperty(r => r.RefreshEndUtc, nowUtc);
                        s.SetProperty(r => r.WasSuccessful, false);
                    }
                    else
                    {
                        s.SetProperty(r => r.PendingManualDecision, decision);
                    }
                });

            return rowsUpdated == 1;
        }

        /// <inheritdoc />
        public async Task ApplyManualCompleteDecision(DataRefreshRecord record)
        {
            record.SetStageCompletionTime(DataRefreshStage.DonorGenotypePrecomputation, DateTime.UtcNow);
            record.PendingManualDecision = null;
            await Context.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task ClearPendingManualDecision(DataRefreshRecord record)
        {
            record.PendingManualDecision = null;
            await Context.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task ClearManualDecisionState(int recordId)
        {
            await Context.DataRefreshRecords
                .Where(r => r.Id == recordId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.AwaitingManualDecisionSinceUtc, (DateTime?) null)
                    .SetProperty(r => r.PendingManualDecision, (DataRefreshManualDecision?) null));
        }

        public async Task<DataRefreshRecord> GetRecord(int recordId)
        {
            return await Context.DataRefreshRecords.SingleAsync(r => r.Id == recordId);
        }

        protected async Task<Dictionary<DataRefreshStage, DateTime?>> GetStageCompletionTimes(int recordId)
        {
            var record = await GetRecord(recordId);
            return EnumExtensions.EnumerateValues<DataRefreshStage>().ToDictionary(
                stage => stage,
                stage => record.GetStageCompletionTime(stage)
            );
        }

        protected async Task<int?> GetLastSuccessfullyInsertedDonor(int recordId)
        {
            var record = await GetRecord(recordId);
            return record.LastSafelyProcessedDonor;
        }

        protected DataRefreshRecord GetLastSuccessfulRecord()
        {
            return Context.DataRefreshRecords
                .Where(r => r.RefreshEndUtc != null && r.WasSuccessful == true)
                .OrderByDescending(r => r.RefreshEndUtc)
                .FirstOrDefault();
        }
    }
}