using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Data.Context;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.ParallelMatchPrediction
{
    /// <summary>
    /// Integration coverage for the abandonment / replay behaviour added to
    /// <see cref="ParallelMatchPredictionRepository"/> (ATL-111).
    /// </summary>
    [TestFixture]
    public class ParallelMatchPredictionRepositoryAbandonmentTests
    {
        private MatchPredictionContext context;
        private IParallelMatchPredictionRepository repository;

        private readonly Fixture fixture = new();

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            context = DependencyInjection.DependencyInjection.Provider.GetService<MatchPredictionContext>();
            repository = new ParallelMatchPredictionRepository(context);
        }

        [TearDown]
        public async Task TearDown()
        {
            await context.ParallelMatchPredictionBatches.ExecuteDeleteAsync();
            await context.ParallelMatchPredictionRuns.ExecuteDeleteAsync();
            context.ChangeTracker.Clear();
        }

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

        [Test]
        public async Task TryMarkRunAsAbandoned_MarksRunAndOnlyTheRequestedBatches()
        {
            var created = await CreateRun(totalBatchCount: 2);
            await repository.RecordBatchResult(created.BatchIdsBySequence[0], fixture.Create<string>()); // batch 0 received

            var header = await repository.TryMarkRunAsAbandoned(created.RunId, DateTime.UtcNow);

            header.Should().NotBeNull();

            var run = (await repository.GetRunWithResults(created.RunId)).Run;
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
            (await repository.GetRunWithResults(runId)).Run.Status
                .Should().Be(ParallelMatchPredictionRunStatus.Running);
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
        public async Task RecordBatchResult_AfterBatchRowDeletedByCleanup_Throws()
        {
            var created = await CreateRun(totalBatchCount: 1);
            await context.ParallelMatchPredictionBatches.Where(b => b.RunId == created.RunId).ExecuteDeleteAsync();

            Func<Task> act = () => repository.RecordBatchResult(created.BatchIdsBySequence[0], fixture.Create<string>());

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

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
        public async Task CleanupBatchesForRunsInitiatedBefore_CleansAbandonedRunAndItsAbandonedBatches()
        {
            var runId = (await CreateRun(totalBatchCount: 2)).RunId;
            await repository.TryMarkRunAsAbandoned(runId, DateTime.UtcNow); // Status -> Abandoned, both batches -> Abandoned
            await BackdateInitiation(runId, DateTime.UtcNow.AddDays(-1));
            (await GetBatchStatuses(runId)).Should().AllBeEquivalentTo(ParallelMatchPredictionBatchStatus.Abandoned);

            await repository.CleanupBatchesForRunsInitiatedBefore(DateTime.UtcNow);

            // Assert on this run specifically (the shared DB may hold rows from other tests, so the global
            // deleted-count is not a reliable per-run assertion).
            var run = (await repository.GetRunWithResults(runId)).Run;
            run.Batches.Should().BeEmpty();
            run.IsCleanedUp.Should().BeTrue();
            // The outcome status is deliberately preserved so the run stays a historical record.
            run.Status.Should().Be(ParallelMatchPredictionRunStatus.Abandoned);
        }

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

        private async Task BackdateInitiation(int runId, DateTime initiatedUtc) =>
            await context.ParallelMatchPredictionRuns
                .Where(r => r.Id == runId)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.MatchPredictionRunInitiatedUtc, initiatedUtc));

        private async Task<List<ParallelMatchPredictionBatchStatus>> GetBatchStatuses(int runId)
        {
            return await context.ParallelMatchPredictionBatches
                .AsNoTracking()
                .Where(b => b.RunId == runId)
                .OrderBy(b => b.BatchSequenceNumber)
                .Select(b => b.BatchStatus)
                .ToListAsync();
        }
    }
}
