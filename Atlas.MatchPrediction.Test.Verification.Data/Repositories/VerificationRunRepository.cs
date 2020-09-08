using Atlas.MatchPrediction.Test.Verification.Data.Context;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification;

namespace Atlas.MatchPrediction.Test.Verification.Data.Repositories
{
    public interface IVerificationRunRepository
    {
        Task<int> AddVerificationRun(VerificationRun run);
        Task MarkSearchRequestsAsSubmitted(int verificationRunId);
    }

    public class VerificationRunRepository : IVerificationRunRepository
    {
        private readonly MatchPredictionVerificationContext context;

        public VerificationRunRepository(MatchPredictionVerificationContext context)
        {
            this.context = context;
        }

        public async Task<int> AddVerificationRun(VerificationRun run)
        {
            await context.VerificationRuns.AddAsync(run);
            await context.SaveChangesAsync();

            return run.Id;
        }

        public async Task MarkSearchRequestsAsSubmitted(int verificationRunId)
        {
            var run = await context.VerificationRuns.FindAsync(verificationRunId);
            run.SearchRequestsSubmitted = true;
            await context.SaveChangesAsync();
        }
    }
}
