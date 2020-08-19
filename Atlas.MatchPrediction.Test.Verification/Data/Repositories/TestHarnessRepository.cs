using System;
using Atlas.MatchPrediction.Test.Verification.Data.Context;
using Atlas.MatchPrediction.Test.Verification.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Verification.Data.Repositories
{
    internal interface ITestHarnessRepository
    {
        Task<int> AddTestHarness(int poolId, string comments);
        Task AddMaskingRecords(IEnumerable<MaskingRecord> records);
        Task MarkAsCompleted(int id);
    }

    internal class TestHarnessRepository : ITestHarnessRepository
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
            var record = await context.TestHarnesses.FindAsync(id);

            if (record == null)
            {
                throw new ArgumentException($"No test harness found with id {id}.");
            }

            record.WasCompleted = true;
            await context.SaveChangesAsync();
        }
    }
}
