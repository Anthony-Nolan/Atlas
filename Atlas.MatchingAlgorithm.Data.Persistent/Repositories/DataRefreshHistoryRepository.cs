using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;

namespace Atlas.MatchingAlgorithm.Data.Persistent.Repositories
{
    public interface IDataRefreshHistoryRepository
    {
        /// <returns>The transient database for which the refresh job was most recently completed</returns>
        TransientDatabase? GetActiveDatabase();
        
        /// <returns>The wmda database version used in the most recently completed refresh job</returns>
        string GetActiveWmdaDataVersion();

        IEnumerable<DataRefreshRecord> GetInProgressJobs();
        Task<int> Create(DataRefreshRecord dataRefreshRecord);
        Task UpdateExecutionDetails(int recordId, string wmdaDataVersion, DateTime? finishTimeUtc);
        Task UpdateSuccessFlag(int recordId, bool wasSuccess);
    }
    
    public class DataRefreshHistoryRepository : IDataRefreshHistoryRepository
    {
        private readonly SearchAlgorithmPersistentContext context;

        public DataRefreshHistoryRepository(SearchAlgorithmPersistentContext context)
        {
            this.context = context;
        }
        
        public TransientDatabase? GetActiveDatabase()
        {
            var lastCompletedRecord = GetLastSuccessfulRecord();
            if (lastCompletedRecord == null)
            {
                return null;
            }
            return (TransientDatabase) Enum.Parse(typeof(TransientDatabase), lastCompletedRecord.Database);
        }

        public string GetActiveWmdaDataVersion()
        {
            var lastCompletedRecord = GetLastSuccessfulRecord();
            return lastCompletedRecord?.WmdaDatabaseVersion;
        }

        public IEnumerable<DataRefreshRecord> GetInProgressJobs()
        {
            return context.DataRefreshRecords.Where(r => r.RefreshEndUtc == null);
        }

        public async Task<int> Create(DataRefreshRecord dataRefreshRecord)
        {
            context.DataRefreshRecords.Add(dataRefreshRecord);
            await context.SaveChangesAsync();
            return dataRefreshRecord.Id;
        }

        public async Task UpdateExecutionDetails(int recordId, string wmdaDataVersion, DateTime? finishTimeUtc)
        {
            var record = await context.DataRefreshRecords.SingleAsync(r => r.Id == recordId);
            record.WmdaDatabaseVersion = wmdaDataVersion;
            if (finishTimeUtc.HasValue)
            {
                record.RefreshEndUtc = finishTimeUtc.Value;
            }
            await context.SaveChangesAsync();
        }

        public async Task UpdateSuccessFlag(int recordId, bool wasSuccess)
        {
            var record = await context.DataRefreshRecords.SingleAsync(r => r.Id == recordId);
            record.WasSuccessful = wasSuccess;
            await context.SaveChangesAsync();
        }

        private DataRefreshRecord GetLastSuccessfulRecord()
        {
            return context.DataRefreshRecords
                .Where(r => r.RefreshEndUtc != null && r.WasSuccessful == true)
                .OrderByDescending(r => r.RefreshEndUtc)
                .FirstOrDefault();
        }
    }
}