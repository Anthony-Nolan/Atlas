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
        Task MarkStageAsComplete(int recordId, DataRefreshStage stage);
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
            await Context.DataRefreshRecords.AddAsync(dataRefreshRecord);
            await Context.SaveChangesAsync();
            return dataRefreshRecord.Id;
        }

        public async Task UpdateExecutionDetails(int recordId, string wmdaHlaNomenclatureVersion, DateTime? finishTimeUtc)
        {
            var record = await GetRecordById(recordId);
            record.HlaNomenclatureVersion = wmdaHlaNomenclatureVersion;
            record.RefreshEndUtc = finishTimeUtc ?? DateTime.UtcNow;
            await Context.SaveChangesAsync();
        }

        public async Task UpdateSuccessFlag(int recordId, bool wasSuccess)
        {
            var record = await GetRecordById(recordId);
            record.WasSuccessful = wasSuccess;
            await Context.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task MarkStageAsComplete(int recordId, DataRefreshStage stage)
        {
            var record = await GetRecordById(recordId);
            switch (stage)
            {
                case DataRefreshStage.DataDeletion:
                    record.DataDeletionCompleted = DateTime.UtcNow;
                    break;
                case DataRefreshStage.DatabaseScalingSetup:
                    record.DatabaseScalingSetupCompleted = DateTime.UtcNow;
                    break;
                case DataRefreshStage.MetadataDictionaryRefresh:
                    record.MetadataDictionaryRefreshCompleted = DateTime.UtcNow;
                    break;
                case DataRefreshStage.DonorImport:
                    record.DonorImportCompleted = DateTime.UtcNow;
                    break;
                case DataRefreshStage.DonorHlaProcessing:
                    record.DonorHlaProcessingCompleted = DateTime.UtcNow;
                    break;
                case DataRefreshStage.DatabaseScalingTearDown:
                    record.DatabaseScalingTearDownCompleted = DateTime.UtcNow;
                    break;
                case DataRefreshStage.QueuedDonorUpdateProcessing:
                    record.QueuedDonorUpdatesCompleted = DateTime.UtcNow;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stage), stage, null);
            }
            await Context.SaveChangesAsync();
        }

        protected async Task<DataRefreshRecord> GetRecordById(int recordId)
        {
            return await Context.DataRefreshRecords.SingleAsync(r => r.Id == recordId);
        }

        private DataRefreshRecord GetLastSuccessfulRecord()
        {
            return Context.DataRefreshRecords
                .Where(r => r.RefreshEndUtc != null && r.WasSuccessful == true)
                .OrderByDescending(r => r.RefreshEndUtc)
                .FirstOrDefault();
        }
    }
}