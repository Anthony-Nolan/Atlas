using System;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.MatchPrediction.Data.Context;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.Repository
{
    /// <summary>
    /// Covers the retention clean-up semantics of <see cref="IParallelMatchPredictionRepository.CleanupBatchesForRunsInitiatedBefore"/>:
    /// runs are cleaned regardless of status once older than the cutoff, their batch rows are deleted and
    /// <see cref="ParallelMatchPredictionRun.IsCleanedUp"/> is set, an in-flight (leased) finalisation is skipped,
    /// and cleaned-up runs are excluded from the finalisation queries.
    /// </summary>
    [TestFixture]
    public class ParallelMatchPredictionRepositoryTests
    {
        private IParallelMatchPredictionRepository repository;
        private MatchPredictionContext context;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                // The shared DI-registered context enables retry-on-failure (via the design-time ContextFactory),
                // whose execution strategy forbids the user-initiated transactions this repository uses. Build a
                // dedicated no-retry context here — matching production (Atlas.Functions/Startup.cs) — so those
                // transactions run, without changing the shared context other integration tests depend on.
                var connectionString = DependencyInjection.DependencyInjection.Provider
                    .GetService<MatchPredictionContext>().Database.GetConnectionString();
                var options = new DbContextOptionsBuilder<MatchPredictionContext>()
                    .UseSqlServer(connectionString)
                    .Options;
                context = new MatchPredictionContext(options);
                repository = new ParallelMatchPredictionRepository(context);
            });
        }

        [SetUp]
        public async Task SetUp()
        {
            // The global DB reset does not touch the parallel tables, so isolate each test explicitly.
            // Delete batches before runs to respect the FK, and clear the tracker so entities created by
            // CreateRun in a prior test do not linger in the shared scoped context.
            context.ChangeTracker.Clear();
            await context.ParallelMatchPredictionBatches.ExecuteDeleteAsync();
            await context.ParallelMatchPredictionRuns.ExecuteDeleteAsync();
        }

        [Test]
        public async Task CleanupBatchesForRunsInitiatedBefore_DeletesBatchesAndFlagsRun_WhenInitiatedBeforeCutoff()
        {
            var cutoff = DateTime.UtcNow;
            var runId = await CreateRunInitiatedAt(cutoff.AddDays(-1), batchCount: 3);

            var deletedCount = await repository.CleanupBatchesForRunsInitiatedBefore(cutoff);

            deletedCount.Should().Be(3);
            (await RemainingBatchCountFor(runId)).Should().Be(0);
            (await IsCleanedUp(runId)).Should().BeTrue();
        }

        [Test]
        public async Task CleanupBatchesForRunsInitiatedBefore_LeavesRunsInitiatedOnOrAfterCutoff()
        {
            var cutoff = DateTime.UtcNow;
            var recentRunId = await CreateRunInitiatedAt(cutoff.AddDays(1), batchCount: 2);

            var deletedCount = await repository.CleanupBatchesForRunsInitiatedBefore(cutoff);

            deletedCount.Should().Be(0);
            (await RemainingBatchCountFor(recentRunId)).Should().Be(2);
            (await IsCleanedUp(recentRunId)).Should().BeFalse();
        }

        [Test]
        public async Task CleanupBatchesForRunsInitiatedBefore_CleansRunsRegardlessOfStatus()
        {
            var cutoff = DateTime.UtcNow;
            var initiatedAt = cutoff.AddDays(-1);

            var abandonedRunningId = await CreateRunInitiatedAt(initiatedAt, batchCount: 1, status: ParallelMatchPredictionRunStatus.Running);
            var finalisedId = await CreateRunInitiatedAt(initiatedAt, batchCount: 1, status: ParallelMatchPredictionRunStatus.Finalised);
            var failedBatchId = await CreateRunInitiatedAt(initiatedAt, batchCount: 1, status: ParallelMatchPredictionRunStatus.FailedDuringBatchProcessing);
            var failedCompletionId = await CreateRunInitiatedAt(initiatedAt, batchCount: 1, status: ParallelMatchPredictionRunStatus.FailedDuringCompletion);

            var deletedCount = await repository.CleanupBatchesForRunsInitiatedBefore(cutoff);

            deletedCount.Should().Be(4);
            foreach (var runId in new[] { abandonedRunningId, finalisedId, failedBatchId, failedCompletionId })
            {
                (await RemainingBatchCountFor(runId)).Should().Be(0);
                (await IsCleanedUp(runId)).Should().BeTrue();
            }
        }

        [Test]
        public async Task CleanupBatchesForRunsInitiatedBefore_SkipsRunningRunClaimedForFinalisation()
        {
            var cutoff = DateTime.UtcNow;
            var leasedRunId = await CreateRunInitiatedAt(
                cutoff.AddDays(-1),
                batchCount: 2,
                status: ParallelMatchPredictionRunStatus.Running,
                finalisationLeaseOwner: Guid.NewGuid());

            var deletedCount = await repository.CleanupBatchesForRunsInitiatedBefore(cutoff);

            // A Running run currently being finalised must not have its batches pulled out from under the
            // completion pipeline.
            deletedCount.Should().Be(0);
            (await RemainingBatchCountFor(leasedRunId)).Should().Be(2);
            (await IsCleanedUp(leasedRunId)).Should().BeFalse();
        }

        [Test]
        public async Task GetRunIdsAwaitingFinalisationAndNotLeased_ExcludesCleanedUpRuns()
        {
            // Two runs identical in every way that matters to the finalisation query (Running, unleased,
            // every batch past Requested); one has already been cleaned up and must be skipped.
            var eligibleRunId = await CreateFinalisationReadyRun();
            var cleanedUpRunId = await CreateFinalisationReadyRun();
            await context.ParallelMatchPredictionRuns
                .Where(r => r.Id == cleanedUpRunId)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsCleanedUp, true));

            var awaiting = await repository.GetRunIdsAwaitingFinalisationAndNotLeased();

            awaiting.Should().Contain(eligibleRunId);
            awaiting.Should().NotContain(cleanedUpRunId);
        }

        // ── helpers ──────────────────────────────────────────────────────────────────

        private async Task<int> CreateRunInitiatedAt(
            DateTime initiatedUtc,
            int batchCount,
            ParallelMatchPredictionRunStatus status = ParallelMatchPredictionRunStatus.Running,
            Guid? finalisationLeaseOwner = null)
        {
            var runId = await repository.CreateRun(new CreateParallelMatchPredictionRunInfo(
                SearchIdentifier: Guid.NewGuid(),
                IsRepeatSearch: false,
                RepeatSearchIdentifier: null,
                ResultsFileName: "results.json",
                ResultsBatched: false,
                BatchFolderName: null,
                MatchingAlgorithmElapsedTime: TimeSpan.FromSeconds(1),
                SearchInitiatedTimeUtc: initiatedUtc,
                TotalBatchCount: batchCount));

            // CreateRun stamps MatchPredictionRunInitiatedUtc with 'now' and status Running; override to
            // position the run relative to the cutoff and to exercise the status-agnostic behaviour.
            await context.ParallelMatchPredictionRuns
                .Where(r => r.Id == runId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.MatchPredictionRunInitiatedUtc, initiatedUtc)
                    .SetProperty(r => r.Status, status)
                    .SetProperty(r => r.FinalisationLeaseOwner, finalisationLeaseOwner));

            return runId;
        }

        private async Task<int> CreateFinalisationReadyRun()
        {
            var runId = await CreateRunInitiatedAt(DateTime.UtcNow, batchCount: 1, status: ParallelMatchPredictionRunStatus.Running);
            await context.ParallelMatchPredictionBatches
                .Where(b => b.RunId == runId)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.BatchStatus, ParallelMatchPredictionBatchStatus.ResultsReceived));
            return runId;
        }

        private async Task<int> RemainingBatchCountFor(int runId) =>
            await context.ParallelMatchPredictionBatches.AsNoTracking().CountAsync(b => b.RunId == runId);

        private async Task<bool> IsCleanedUp(int runId) =>
            await context.ParallelMatchPredictionRuns.AsNoTracking().Where(r => r.Id == runId).Select(r => r.IsCleanedUp).SingleAsync();
    }
}
