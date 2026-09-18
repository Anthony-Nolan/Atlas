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

        IEnumerable<DataRefreshRecord> GetIncompleteRefreshJobs();
        Task<int> Create(DataRefreshRecord dataRefreshRecord);

        Task<DataRefreshRecord> GetRecord(int dataRefreshRecordId);
        Task UpdateRunAttemptDetails(int recordId);
        Task UpdateExecutionDetails(int recordId, string wmdaHlaNomenclatureVersion, DateTime? finishTimeUtc = null);
        Task UpdateSuccessFlag(int recordId, bool wasSuccess);
        Task UpdateLastSafelyProcessedDonor(int recordId, int donorId);
        Task MarkStageAsComplete(DataRefreshRecord record, DataRefreshStage stage);

        /// <summary>
        /// Attempts to take the run-level lease on a record, so that only one invocation processes it at a time.
        /// The claim succeeds if the record is still open and is either unleased, leased to <paramref name="owner"/>
        /// already, or held by an owner whose lease has expired.
        /// </summary>
        /// <remarks>
        /// Re-claiming for the current <paramref name="owner"/> is deliberately permitted, so that a message redelivered
        /// to the invocation that already holds the lease is harmless.
        /// </remarks>
        /// <returns>True if this owner now holds the lease. False if another invocation holds it, or the record is already finished.</returns>
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
        /// </remarks>
        /// <param name="graceCutoffUtc">The record must have been idle since before this time to count as stalled.</param>
        Task<IReadOnlyCollection<int>> GetStalledRefreshRecordIds(DateTime graceCutoffUtc);
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

        public IEnumerable<DataRefreshRecord> GetIncompleteRefreshJobs()
        {
            return Context.DataRefreshRecords.Where(r => r.RefreshEndUtc == null);
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
        public async Task<bool> TryClaimRefreshLease(int recordId, Guid owner, DateTime nowUtc, TimeSpan ttl)
        {
            var expiry = nowUtc + ttl;

            var rowsUpdated = await Context.DataRefreshRecords
                .Where(r => r.Id == recordId
                            && r.RefreshEndUtc == null
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
                            && (r.LeaseOwner == null || r.LeaseExpiresUtc < graceCutoffUtc)
                            && (r.RefreshLastContinuedUtc ?? r.RefreshRequestedUtc) < graceCutoffUtc)
                .Select(r => r.Id)
                .ToListAsync();
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