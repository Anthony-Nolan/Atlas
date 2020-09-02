using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Data.Persistent.Context;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using EnumStringValues;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;

namespace Atlas.MatchingAlgorithm.Data.Persistent.Repositories
{
    public interface IDataRefreshHistoryRepository
    {
        /// <returns>The transient database for which the refresh job was most recently completed</returns>
        TransientDatabase? GetActiveDatabase();

        /// <returns>The HLA nomenclature version used in the most recently completed refresh job</returns>
        string GetActiveHlaNomenclatureVersion();

        IEnumerable<DataRefreshRecord> GetInProgressJobs();
        Task<int> Create(DataRefreshRecord dataRefreshRecord);

        Task<DataRefreshRecord> GetRecord(int dataRefreshRecordId);
        Task UpdateExecutionDetails(int recordId, string wmdaHlaNomenclatureVersion, DateTime? finishTimeUtc = null);
        Task UpdateSuccessFlag(int recordId, bool wasSuccess);
        Task UpdateLastSafelyProcessedDonor(int recordId, int donorId);
        Task MarkStageAsComplete(DataRefreshRecord record, DataRefreshStage stage);
        Task MarkJobAsContinued(int recordId);
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

        public IEnumerable<DataRefreshRecord> GetInProgressJobs()
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

        public async Task UpdateExecutionDetails(int recordId, string wmdaHlaNomenclatureVersion, DateTime? finishTimeUtc)
        {
            var record = await GetRecord(recordId);
            record.HlaNomenclatureVersion = wmdaHlaNomenclatureVersion ?? record.HlaNomenclatureVersion; // Don't wipe the HLA version if we already recorded it.
            record.RefreshEndUtc = finishTimeUtc;
            await Context.SaveChangesAsync();
        }

        public async Task MarkJobAsContinued(int recordId)
        {
            var record = await GetRecord(recordId);
            record.RefreshContinueUtc = DateTime.UtcNow;
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