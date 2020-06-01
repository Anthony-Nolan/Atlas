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

        IEnumerable<DataRefreshRecord> GetInProgressJobs();
        Task<int> Create(DataRefreshRecord dataRefreshRecord);
        Task UpdateExecutionDetails(int recordId, string wmdaHlaNomenclatureVersion, DateTime? finishTimeUtc);
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

            return lastCompletedRecord?.Database.ParseToEnum<TransientDatabase>();
        }

        public string GetActiveHlaNomenclatureVersion()
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

        public async Task UpdateExecutionDetails(int recordId, string wmdaHlaNomenclatureVersion, DateTime? finishTimeUtc)
        {
            var record = await context.DataRefreshRecords.SingleAsync(r => r.Id == recordId);
            record.WmdaDatabaseVersion = wmdaHlaNomenclatureVersion;
            record.RefreshEndUtc = finishTimeUtc.Value;
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