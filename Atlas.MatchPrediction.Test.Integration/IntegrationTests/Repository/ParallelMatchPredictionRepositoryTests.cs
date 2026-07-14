using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.MatchPrediction.Data.Context;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.Repository
{
    /// <summary>
    /// Integration coverage for <see cref="ParallelMatchPredictionRepository"/>, grouped by method under test:
    /// run status transitions, batch result/failure recording (including duplicate delivery and late replay after
    /// abandonment — ATL-111), abandonment sweeps, finalisation eligibility, and retention clean-up.
    /// </summary>
    [TestFixture]
    public class ParallelMatchPredictionRepositoryTests
    {
        private IParallelMatchPredictionRepository repository;
        private MatchPredictionContext context;

        private readonly Fixture fixture = new();

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

        // ── MarkRunFinalised ─────────────────────────────────────────────────────────

        [Test]
        public async Task MarkRunFinalised_WhenRunning_SetsFinalisedAndSuccessful()
        {
            var runId = (await CreateRun(totalBatchCount: 1)).RunId;
            var finalisedTime = DateTime.UtcNow;

            await repository.MarkRunFinalised(runId, finalisedTime);

            var run = await LoadRun(runId);
            run.Status.Should().Be(ParallelMatchPredictionRunStatus.Finalised);
            run.IsSuccessful.Should().BeTrue();
            run.FinalisedTimeUtc.Should().BeCloseTo(finalisedTime, TimeSpan.FromSeconds(1));
        }

        [Test]
        public async Task MarkRunFinalised_WhenAbandoned_ReplaysToFinalised()
        {
            var runId = (await CreateRun(totalBatchCount: 1)).RunId;
            await repository.TryMarkRunAsAbandoned(runId, DateTime.UtcNow);

            await repository.MarkRunFinalised(runId, DateTime.UtcNow);

            (await LoadRun(runId)).Status.Should().Be(ParallelMatchPredictionRunStatus.Finalised);
        }

        [Test]
        public async Task MarkRunFinalised_WhenRunAlreadyTerminal_IsNoOp()
        {
            var runId = (await CreateRun(totalBatchCount: 1)).RunId;
            await repository.MarkRunFailedDuringCompletion(runId, DateTime.UtcNow);

            // The finaliser guard only matches Running/Abandoned runs, so a terminal run must not be flipped to Finalised.
            await repository.MarkRunFinalised(runId, DateTime.UtcNow);

            var run = await LoadRun(runId);
            run.Status.Should().Be(ParallelMatchPredictionRunStatus.FailedDuringCompletion);
            run.IsSuccessful.Should().BeNull();
        }

        // ── MarkRunFailed ────────────────────────────────────────────────────────────

        [Test]
        public async Task MarkRunFailed_WhenRunning_SetsFailedDuringBatchProcessingAndNotSuccessful()
        {
            var runId = (await CreateRun(totalBatchCount: 1)).RunId;

            await repository.MarkRunFailed(runId, DateTime.UtcNow);

            var run = await LoadRun(runId);
            run.Status.Should().Be(ParallelMatchPredictionRunStatus.FailedDuringBatchProcessing);
            run.IsSuccessful.Should().BeFalse();
        }

        [Test]
        public async Task MarkRunFailed_WhenAbandoned_SetsFailedDuringBatchProcessing()
        {
            var runId = (await CreateRun(totalBatchCount: 1)).RunId;
            await repository.TryMarkRunAsAbandoned(runId, DateTime.UtcNow);

            await repository.MarkRunFailed(runId, DateTime.UtcNow);

            (await LoadRun(runId)).Status.Should().Be(ParallelMatchPredictionRunStatus.FailedDuringBatchProcessing);
        }

        // ── MarkRunFailedDuringCompletion ────────────────────────────────────────────

        [Test]
        public async Task MarkRunFailedDuringCompletion_WhenRunning_SetsFailedDuringCompletion()
        {
            var runId = (await CreateRun(totalBatchCount: 1)).RunId;

            await repository.MarkRunFailedDuringCompletion(runId, DateTime.UtcNow);

            (await LoadRun(runId)).Status.Should().Be(ParallelMatchPredictionRunStatus.FailedDuringCompletion);
        }

        [Test]
        public async Task MarkRunFailedDuringCompletion_WhenAbandoned_SetsFailedDuringCompletion()
        {
            var runId = (await CreateRun(totalBatchCount: 1)).RunId;
            await repository.TryMarkRunAsAbandoned(runId, DateTime.UtcNow);

            await repository.MarkRunFailedDuringCompletion(runId, DateTime.UtcNow);

            (await LoadRun(runId)).Status.Should().Be(ParallelMatchPredictionRunStatus.FailedDuringCompletion);
        }

        // ── RecordBatchResult ────────────────────────────────────────────────────────

        [Test]
        public async Task RecordBatchResult_WhenDuplicateDelivery_ReturnsFalseAndLeavesStatusReceived()
        {
            var created = await CreateRun(totalBatchCount: 1);
            var batchId = created.BatchIdsBySequence[0];

            (await repository.RecordBatchResult(batchId, fixture.Create<string>())).Should().BeTrue();
            (await repository.RecordBatchResult(batchId, fixture.Create<string>())).Should().BeFalse();

            (await GetBatchStatus(batchId)).Should().Be(ParallelMatchPredictionBatchStatus.ResultsReceived);
        }

        [Test]
        public async Task RecordBatchResult_ForAbandonedBatch_RecordsTheLateResult()
        {
            var created = await CreateRun(totalBatchCount: 1);
            await repository.TryMarkRunAsAbandoned(created.RunId, DateTime.UtcNow); // batch -> Abandoned

            var recorded = await repository.RecordBatchResult(created.BatchIdsBySequence[0], fixture.Create<string>());

            recorded.Should().BeTrue();
            (await GetBatchStatuses(created.RunId)).Should().ContainSingle()
                .Which.Should().Be(ParallelMatchPredictionBatchStatus.ResultsReceived);
        }

        [Test]
        public async Task RecordBatchResult_WhenBatchRowMissing_Throws()
        {
            var created = await CreateRun(totalBatchCount: 1);
            await context.ParallelMatchPredictionBatches.Where(b => b.RunId == created.RunId).ExecuteDeleteAsync();

            Func<Task> act = () => repository.RecordBatchResult(created.BatchIdsBySequence[0], fixture.Create<string>());

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        // ── RecordBatchFailure ───────────────────────────────────────────────────────

        [Test]
        public async Task RecordBatchFailure_WhenDuplicateDelivery_ReturnsFalseAndLeavesStatusFailed()
        {
            var created = await CreateRun(totalBatchCount: 1);
            var batchId = created.BatchIdsBySequence[0];

            (await repository.RecordBatchFailure(batchId, fixture.Create<string>(), fixture.Create<string>())).Should().BeTrue();
            (await repository.RecordBatchFailure(batchId, fixture.Create<string>(), fixture.Create<string>())).Should().BeFalse();

            (await GetBatchStatus(batchId)).Should().Be(ParallelMatchPredictionBatchStatus.Failed);
        }

        [Test]
        public async Task RecordBatchFailure_ForAbandonedBatch_RecordsLateFailureAndRunBecomesFinalisationEligible()
        {
            var created = await CreateRun(totalBatchCount: 1);
            await repository.TryMarkRunAsAbandoned(created.RunId, DateTime.UtcNow); // batch -> Abandoned

            // Late failure replay: the previously-missing batch returns a failure (e.g. an OOM) after abandonment.
            var recorded = await repository.RecordBatchFailure(created.BatchIdsBySequence[0], fixture.Create<string>(), fixture.Create<string>());

            recorded.Should().BeTrue();
            (await GetBatchStatuses(created.RunId)).Should().ContainSingle()
                .Which.Should().Be(ParallelMatchPredictionBatchStatus.Failed);

            // The run stays Abandoned but is now finalisation-eligible (no Requested/Abandoned batches remain), so the
            // finaliser re-picks it and the completion service republishes the definitive failure (with the real
            // batch-failure cause) and transitions the run to FailedDuringBatchProcessing.
            var runResults = await repository.GetRunWithResults(created.RunId);
            runResults.Run.Status.Should().Be(ParallelMatchPredictionRunStatus.Abandoned);
            runResults.FailedBatches.Should().ContainSingle();
            (await repository.GetRunIdsAwaitingFinalisationAndNotLeased()).Should().Contain(created.RunId);
        }

        [Test]
        public async Task RecordBatchFailure_WhenBatchRowMissing_Throws()
        {
            var created = await CreateRun(totalBatchCount: 1);
            await context.ParallelMatchPredictionBatches.Where(b => b.RunId == created.RunId).ExecuteDeleteAsync();

            Func<Task> act = () => repository.RecordBatchFailure(created.BatchIdsBySequence[0], fixture.Create<string>(), fixture.Create<string>());

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        // ── GetRunWithResults ────────────────────────────────────────────────────────

        [Test]
        public async Task GetRunWithResults_WhenRunDoesNotExist_ReturnsNull()
        {
            // Identity ids are positive, so a negative id can never exist.
            (await repository.GetRunWithResults(-1)).Should().BeNull();
        }

        // ── GetRunIdsToAbandon ───────────────────────────────────────────────────────

        [Test]
        public async Task GetRunIdsToAbandon_RunInitiatedBeforeCutoffWithRequestedBatch_IsReturned()
        {
            var created = await CreateRun(totalBatchCount: 2);
            await repository.RecordBatchResult(created.BatchIdsBySequence[0], fixture.Create<string>()); // batch 1 still Requested

            var result = await repository.GetRunIdsToAbandon(DateTime.UtcNow.AddMinutes(1));

            result.Should().Contain(created.RunId);
        }

        [Test]
        public async Task GetRunIdsToAbandon_RunWithEveryBatchReceived_IsNotReturned()
        {
            var created = await CreateRun(totalBatchCount: 2);
            await repository.RecordBatchResult(created.BatchIdsBySequence[0], fixture.Create<string>());
            await repository.RecordBatchResult(created.BatchIdsBySequence[1], fixture.Create<string>());

            var result = await repository.GetRunIdsToAbandon(DateTime.UtcNow.AddMinutes(1));

            result.Should().NotContain(created.RunId);
        }

        [Test]
        public async Task GetRunIdsToAbandon_RunInitiatedAfterCutoff_IsNotReturned()
        {
            var runId = (await CreateRun(totalBatchCount: 1)).RunId;

            // Cutoff is in the past, so a run initiated "now" has not yet timed out.
            var result = await repository.GetRunIdsToAbandon(DateTime.UtcNow.AddMinutes(-1));

            result.Should().NotContain(runId);
        }

        // ── TryMarkRunAsAbandoned ────────────────────────────────────────────────────

        [Test]
        public async Task TryMarkRunAsAbandoned_MarksRunAndOnlyTheRequestedBatches()
        {
            var created = await CreateRun(totalBatchCount: 2);
            await repository.RecordBatchResult(created.BatchIdsBySequence[0], fixture.Create<string>()); // batch 0 received

            var header = await repository.TryMarkRunAsAbandoned(created.RunId, DateTime.UtcNow);

            header.Should().NotBeNull();

            var run = await LoadRun(created.RunId);
            run.Status.Should().Be(ParallelMatchPredictionRunStatus.Abandoned);
            run.IsSuccessful.Should().BeFalse();

            var batchStatuses = await GetBatchStatuses(created.RunId);
            batchStatuses.Should().BeEquivalentTo(new[]
            {
                ParallelMatchPredictionBatchStatus.ResultsReceived,
                ParallelMatchPredictionBatchStatus.Abandoned,
            });
        }

        [Test]
        public async Task TryMarkRunAsAbandoned_CalledTwice_SecondCallReturnsNull()
        {
            var runId = (await CreateRun(totalBatchCount: 1)).RunId;

            (await repository.TryMarkRunAsAbandoned(runId, DateTime.UtcNow)).Should().NotBeNull();
            (await repository.TryMarkRunAsAbandoned(runId, DateTime.UtcNow)).Should().BeNull();
        }

        [Test]
        public async Task TryMarkRunAsAbandoned_WhenRunAlreadyLeasedByFinaliser_DoesNotAbandon()
        {
            var runId = (await CreateRun(totalBatchCount: 1)).RunId;
            // A finaliser claims the run (e.g. after a late result arrived) before the abandonment sweep acts on it.
            (await repository.TryClaimFinalisationLease(runId, fixture.Create<Guid>())).Should().BeTrue();

            var header = await repository.TryMarkRunAsAbandoned(runId, DateTime.UtcNow);

            header.Should().BeNull();
            (await LoadRun(runId)).Status.Should().Be(ParallelMatchPredictionRunStatus.Running);
        }

        // ── GetRunIdsAwaitingFinalisationAndNotLeased ────────────────────────────────

        [Test]
        public async Task GetRunIdsAwaitingFinalisationAndNotLeased_AbandonedRun_BecomesEligibleOnceAllLateResultsArrive()
        {
            var created = await CreateRun(totalBatchCount: 1);
            await repository.TryMarkRunAsAbandoned(created.RunId, DateTime.UtcNow); // run + batch Abandoned

            // Still has an Abandoned batch, so not yet finalisable.
            (await repository.GetRunIdsAwaitingFinalisationAndNotLeased()).Should().NotContain(created.RunId);

            await repository.RecordBatchResult(created.BatchIdsBySequence[0], fixture.Create<string>()); // late result arrives

            (await repository.GetRunIdsAwaitingFinalisationAndNotLeased()).Should().Contain(created.RunId);
        }

        [Test]
        public async Task GetRunIdsAwaitingFinalisationAndNotLeased_AbandonedRunWithAnyBatchStillAbandoned_IsNotEligible()
        {
            var created = await CreateRun(totalBatchCount: 2);
            await repository.TryMarkRunAsAbandoned(created.RunId, DateTime.UtcNow); // both batches -> Abandoned

            // Only one of the two abandoned batches gets a late result; the other stays Abandoned.
            await repository.RecordBatchResult(created.BatchIdsBySequence[0], fixture.Create<string>());
            (await GetBatchStatuses(created.RunId)).Should().BeEquivalentTo(new[]
            {
                ParallelMatchPredictionBatchStatus.ResultsReceived,
                ParallelMatchPredictionBatchStatus.Abandoned,
            });

            // A single lingering Abandoned batch keeps the already-reported run out of finalisation — it must not be
            // re-finalised (as bogus success) while a batch is still outstanding.
            (await repository.GetRunIdsAwaitingFinalisationAndNotLeased()).Should().NotContain(created.RunId);

            // Only once every abandoned batch has been superseded by a late result does the run become eligible (replay).
            await repository.RecordBatchResult(created.BatchIdsBySequence[1], fixture.Create<string>());
            (await repository.GetRunIdsAwaitingFinalisationAndNotLeased()).Should().Contain(created.RunId);
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

        // ── CleanupBatchesForRunsInitiatedBefore ─────────────────────────────────────

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

            var runningId = await CreateRunInitiatedAt(initiatedAt, batchCount: 1, status: ParallelMatchPredictionRunStatus.Running);
            var finalisedId = await CreateRunInitiatedAt(initiatedAt, batchCount: 1, status: ParallelMatchPredictionRunStatus.Finalised);
            var failedBatchId = await CreateRunInitiatedAt(initiatedAt, batchCount: 1, status: ParallelMatchPredictionRunStatus.FailedDuringBatchProcessing);
            var failedCompletionId = await CreateRunInitiatedAt(initiatedAt, batchCount: 1, status: ParallelMatchPredictionRunStatus.FailedDuringCompletion);

            var deletedCount = await repository.CleanupBatchesForRunsInitiatedBefore(cutoff);

            deletedCount.Should().Be(4);
            foreach (var runId in new[] { runningId, finalisedId, failedBatchId, failedCompletionId })
            {
                (await RemainingBatchCountFor(runId)).Should().Be(0);
                (await IsCleanedUp(runId)).Should().BeTrue();
            }
        }

        [Test]
        public async Task CleanupBatchesForRunsInitiatedBefore_CleansAbandonedRunAndItsAbandonedBatches()
        {
            var runId = (await CreateRun(totalBatchCount: 2)).RunId;
            await repository.TryMarkRunAsAbandoned(runId, DateTime.UtcNow); // Status -> Abandoned, both batches -> Abandoned
            await BackdateInitiation(runId, DateTime.UtcNow.AddDays(-1));
            (await GetBatchStatuses(runId)).Should().AllBeEquivalentTo(ParallelMatchPredictionBatchStatus.Abandoned);

            await repository.CleanupBatchesForRunsInitiatedBefore(DateTime.UtcNow);

            var run = await LoadRun(runId);
            run.Batches.Should().BeEmpty();
            run.IsCleanedUp.Should().BeTrue();
            // The outcome status is deliberately preserved so the run stays a historical record.
            run.Status.Should().Be(ParallelMatchPredictionRunStatus.Abandoned);
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

        // ── helpers ──────────────────────────────────────────────────────────────────

        private async Task<CreateParallelMatchPredictionRunResult> CreateRun(int totalBatchCount)
        {
            return await repository.CreateRun(new CreateParallelMatchPredictionRunInfo(
                SearchIdentifier: fixture.Create<Guid>(),
                IsRepeatSearch: false,
                RepeatSearchIdentifier: null,
                ResultsFileName: fixture.Create<string>(),
                ResultsBatched: false,
                // BatchFolderName has a 36-char limit; a GUID string fits it exactly.
                BatchFolderName: fixture.Create<Guid>().ToString(),
                MatchingAlgorithmElapsedTime: fixture.Create<TimeSpan>(),
                SearchInitiatedTimeUtc: fixture.Create<DateTime>(),
                TotalBatchCount: totalBatchCount
            ));
        }

        private async Task<int> CreateRunInitiatedAt(
            DateTime initiatedUtc,
            int batchCount,
            ParallelMatchPredictionRunStatus status = ParallelMatchPredictionRunStatus.Running,
            Guid? finalisationLeaseOwner = null)
        {
            var runId = (await CreateRun(batchCount)).RunId;

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

        private async Task<ParallelMatchPredictionRun> LoadRun(int runId) =>
            (await repository.GetRunWithResults(runId)).Run;

        private async Task BackdateInitiation(int runId, DateTime initiatedUtc) =>
            await context.ParallelMatchPredictionRuns
                .Where(r => r.Id == runId)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.MatchPredictionRunInitiatedUtc, initiatedUtc));

        private async Task<ParallelMatchPredictionBatchStatus> GetBatchStatus(int batchId) =>
            await context.ParallelMatchPredictionBatches
                .AsNoTracking()
                .Where(b => b.Id == batchId)
                .Select(b => b.BatchStatus)
                .SingleAsync();

        private async Task<List<ParallelMatchPredictionBatchStatus>> GetBatchStatuses(int runId)
        {
            return await context.ParallelMatchPredictionBatches
                .AsNoTracking()
                .Where(b => b.RunId == runId)
                .OrderBy(b => b.BatchSequenceNumber)
                .Select(b => b.BatchStatus)
                .ToListAsync();
        }

        private async Task<int> RemainingBatchCountFor(int runId) =>
            await context.ParallelMatchPredictionBatches.AsNoTracking().CountAsync(b => b.RunId == runId);

        private async Task<bool> IsCleanedUp(int runId) =>
            await context.ParallelMatchPredictionRuns.AsNoTracking().Where(r => r.Id == runId).Select(r => r.IsCleanedUp).SingleAsync();
    }
}
