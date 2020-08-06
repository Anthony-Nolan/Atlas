using Atlas.MatchPrediction.Test.Verification.Data.Context;
using Atlas.MatchPrediction.Test.Verification.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Verification.Data.Repositories
{
    public interface ITestHarnessRepository
    {
        Task<int> AddTestHarness(int poolId);
        Task AddMaskingRecords(IEnumerable<MaskingRecord> records);
    }

    internal class TestHarnessRepository : ITestHarnessRepository
    {
        private readonly MatchPredictionVerificationContext context;

        public TestHarnessRepository(MatchPredictionVerificationContext context)
        {
            this.context = context;
        }

        public async Task<int> AddTestHarness(int poolId)
        {
            var harness = new TestHarness
            {
                NormalisedPool_Id = poolId
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
    }
}
