using System;
using Atlas.MatchPrediction.Test.Verification.Data.Context;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Test.Verification.Data.Models.TestHarness;

namespace Atlas.MatchPrediction.Test.Verification.Data.Repositories
{
    public interface ITestHarnessRepository
    {
        Task<int> AddTestHarness(int poolId, string comments);
        Task AddMaskingRecords(IEnumerable<MaskingRecord> records);
        Task MarkAsCompleted(int id);
        Task<bool> WasTestHarnessCompleted(int id);
        Task<int> GetHaplotypeFrequencySetIdOfTestHarness(int id);
    }

    public class TestHarnessRepository : ITestHarnessRepository
    {
        private readonly MatchPredictionVerificationContext context;

        public TestHarnessRepository(MatchPredictionVerificationContext context)
        {
            this.context = context;
        }

        public async Task<int> AddTestHarness(int poolId, string comments)
        {
            var harness = new TestHarness
            {
                NormalisedPool_Id = poolId,
                Comments = comments
            };

            await context.TestHarnesses.AddAsync(harness);
            await context.SaveChangesAsync();

            return harness.Id;
        }

        public async Task AddMaskingRecords(IEnumerable<MaskingRecord> records)
        {
            await context.MaskingRecords.AddRangeAsync(records);
            await context.SaveChangesAsync();
        }

        public async Task MarkAsCompleted(int id)
        {
            var record = await GetTestHarness(id);

            if (record == null)
            {
                throw new ArgumentException($"No test harness found with id {id}.");
            }

            record.WasCompleted = true;
            await context.SaveChangesAsync();
        }

        public async Task<bool> WasTestHarnessCompleted(int id)
        {
            var record = await GetTestHarness(id);

            if (record == null)
            {
                throw new ArgumentException($"No test harness found with id {id}.");
            }

            return record.WasCompleted;
        }

        public async Task<int> GetHaplotypeFrequencySetIdOfTestHarness(int id)
        {
            var harness = await GetTestHarness(id);
            var pool = await context.NormalisedPool.FindAsync(harness.NormalisedPool_Id);
            return pool.HaplotypeFrequencySetId;
        }

        private async Task<TestHarness> GetTestHarness(int id)
        {
            return await context.TestHarnesses.FindAsync(id);
        }
    }
}
