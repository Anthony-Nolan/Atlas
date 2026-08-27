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
        Task<bool> TryRenewRefreshLease(int recordId, Guid owner, DateTime nowUtc, TimeSpan ttl);

        /// <summary>
        /// Gives up the lease, provided <paramref name="owner"/> still holds it, so the next invocation need not wait
        /// out the remaining lease duration.
        /// </summary>
        /// <returns>False if this owner had already been fenced, and so released nothing.</returns>
        Task<bool> ReleaseRefreshLease(int recordId, Guid owner);
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
        public async Task<bool> TryRenewRefreshLease(int recordId, Guid owner, DateTime nowUtc, TimeSpan ttl)
        {
            var expiry = nowUtc + ttl;

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