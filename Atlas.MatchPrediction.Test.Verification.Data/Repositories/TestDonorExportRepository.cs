using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.MatchPrediction.Test.Verification.Data.Context;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Client.Models.DataRefresh;
using Atlas.MatchPrediction.Test.Verification.Data.Models.TestHarness;
using Microsoft.EntityFrameworkCore;

namespace Atlas.MatchPrediction.Test.Verification.Data.Repositories
{
    public interface ITestDonorExportRepository
    {
        Task<IEnumerable<TestDonorExportRecord>> GetRecordsWithoutDataRefreshDetails();
        Task<int> AddRecord(int testHarnessId);
        Task SetExportedDateTime(int id);
        Task UpdateLatestRecordWithDataRefreshDetails(CompletedDataRefresh dataRefresh);
        Task<TestDonorExportRecord> GetLastExportRecord();
    }

    public class TestDonorExportRepository : ITestDonorExportRepository
    {
        private readonly MatchPredictionVerificationContext context;

        public TestDonorExportRepository(MatchPredictionVerificationContext context)
        {
            this.context = context;
        }

        public async Task<IEnumerable<TestDonorExportRecord>> GetRecordsWithoutDataRefreshDetails()
        {
            return await context.TestDonorExportRecords
                .AsQueryable()
                .Where(t => t.DataRefreshRecordId == null)
                .ToListAsync();
        }

        public async Task<int> AddRecord(int testHarnessId)
        {
            var record = new TestDonorExportRecord
            {
                TestHarness_Id = testHarnessId
            };

            context.TestDonorExportRecords.Add(record);
            await context.SaveChangesAsync();

            return record.Id;
        }

        public async Task SetExportedDateTime(int id)
        {
            var record = await context.TestDonorExportRecords.FindAsync(id);

            if (record == null)
            {
                throw new Exception("No test donor export record found.");
            }

            record.Exported = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync();
        }

        public async Task UpdateLatestRecordWithDataRefreshDetails(CompletedDataRefresh dataRefresh)
        {
            var latestRecord = await GetLastExportRecord();

            if (latestRecord == null)
            {
                throw new Exception("No test donor export record found.");
            }

            if (latestRecord.DataRefreshRecordId != null)
            {
                throw new Exception("Latest record already has data refresh details.");
            }

            latestRecord.DataRefreshCompleted = DateTimeOffset.UtcNow;
            latestRecord.DataRefreshRecordId = dataRefresh.DataRefreshRecordId;
            latestRecord.WasDataRefreshSuccessful = dataRefresh.WasSuccessful;

            await context.SaveChangesAsync();
        }

        public async Task<TestDonorExportRecord> GetLastExportRecord()
        {
            return await context.TestDonorExportRecords
                .AsQueryable()
                .OrderBy(t => t.Id)
                .LastOrDefaultAsync();
        }
    }
}
