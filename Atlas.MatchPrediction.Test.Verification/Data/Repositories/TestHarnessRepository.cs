using Atlas.MatchPrediction.Test.Verification.Data.Context;
using Atlas.MatchPrediction.Test.Verification.Data.Models;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Verification.Data.Repositories
{
    public interface ITestHarnessRepository
    {
        Task<int> AddTestHarness(int poolId);
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
    }
}
