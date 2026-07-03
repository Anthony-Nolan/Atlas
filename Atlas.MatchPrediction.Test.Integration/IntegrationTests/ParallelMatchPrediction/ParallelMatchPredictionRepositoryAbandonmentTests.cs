using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Data.Context;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using AutoFixture;
using FluentAssertions;
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
            var runId = await CreateRun(totalBatchCount: 2);
            await repository.RecordBatchResult(runId, 0, new Dictionary<int, string>()); // batch 1 still Requested

            var result = await repository.GetRunIdsToAbandon(DateTime.UtcNow.AddMinutes(1));

            result.Should().Contain(runId);
        }

        [Test]
        public async Task GetRunIdsToAbandon_RunWithEveryBatchReceived_IsNotReturned()
        {
            var runId = await CreateRun(totalBatchCount: 2);
            await repository.RecordBatchResult(runId, 0, new Dictionary<int, string>());
            await repository.RecordBatchResult(runId, 1, new Dictionary<int, string>());

            var result = await repository.GetRunIdsToAbandon(DateTime.UtcNow.AddMinutes(1));

            result.Should().NotContain(runId);
        }

        [Test]
        public async Task GetRunIdsToAbandon_RunInitiatedAfterCutoff_IsNotReturned()
        {
            var runId = await CreateRun(totalBatchCount: 1);

            // Cutoff is in the past, so a run initiated "now" has not yet timed out.
            var result = await repository.GetRunIdsToAbandon(DateTime.UtcNow.AddMinutes(-1));

            result.Should().NotContain(runId);
        }

        [Test]
        public async Task TryMarkRunAsAbandoned_MarksRunAndOnlyTheRequestedBatches()
        {
            var runId = await CreateRun(totalBatchCount: 2);
            await repository.RecordBatchResult(runId, 0, new Dictionary<int, string>()); // batch 0 received

            var header = await repository.TryMarkRunAsAbandoned(runId, DateTime.UtcNow);

            header.Should().NotBeNull();

            var run = (await repository.GetRunWithResults(runId)).Run;
            run.Status.Should().Be(ParallelMatchPredictionRunStatus.Abandoned);
            run.IsSuccessful.Should().BeFalse();

            var batchStatuses = await GetBatchStatuses(runId);
            batchStatuses.Should().BeEquivalentTo(new[]
            {
                ParallelMatchPredictionBatchStatus.ResultsReceived,
                ParallelMatchPredictionBatchStatus.Abandoned,
            });
        }

        [Test]
        public async Task TryMarkRunAsAbandoned_CalledTwice_SecondCallReturnsNull()
        {
            var runId = await CreateRun(totalBatchCount: 1);

            (await repository.TryMarkRunAsAbandoned(runId, DateTime.UtcNow)).Should().NotBeNull();
            (await repository.TryMarkRunAsAbandoned(runId, DateTime.UtcNow)).Should().BeNull();
        }

        [Test]
        public async Task TryMarkRunAsAbandoned_WhenRunAlreadyLeasedByFinaliser_DoesNotAbandon()
        {
            var runId = await CreateRun(totalBatchCount: 1);
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
            var runId = await CreateRun(totalBatchCount: 1);
            await repository.TryMarkRunAsAbandoned(runId, DateTime.UtcNow); // batch -> Abandoned

            var recorded = await repository.RecordBatchResult(runId, 0, new Dictionary<int, string>());

            recorded.Should().BeTrue();
            (await GetBatchStatuses(runId)).Should().ContainSingle()
                .Which.Should().Be(ParallelMatchPredictionBatchStatus.ResultsReceived);
        }

        [Test]
        public async Task RecordBatchFailure_ForAbandonedBatch_RecordsLateFailureAndRunBecomesFinalisationEligible()
        {
            var runId = await CreateRun(totalBatchCount: 1);
            await repository.TryMarkRunAsAbandoned(runId, DateTime.UtcNow); // batch -> Abandoned

            // Late failure replay: the previously-missing batch returns a failure (e.g. an OOM) after abandonment.
            var recorded = await repository.RecordBatchFailure(runId, 0, fixture.Create<string>(), fixture.Create<string>());

            recorded.Should().BeTrue();
            (await GetBatchStatuses(runId)).Should().ContainSingle()
                .Which.Should().Be(ParallelMatchPredictionBatchStatus.Failed);

            // The run stays Abandoned but is now finalisation-eligible (no Requested/Abandoned batches remain), so the
            // finaliser re-picks it and the completion service republishes the definitive failure (with the real
            // batch-failure cause) and transitions the run to FailedDuringBatchProcessing.
            var runResults = await repository.GetRunWithResults(runId);
            runResults.Run.Status.Should().Be(ParallelMatchPredictionRunStatus.Abandoned);
            runResults.FailedBatches.Should().ContainSingle();
            (await repository.GetRunIdsAwaitingFinalisationAndNotLeased()).Should().Contain(runId);
        }

        [Test]
        public async Task RecordBatchResult_AfterBatchRowDeletedByCleanup_Throws()
        {
            var runId = await CreateRun(totalBatchCount: 1);
            await context.ParallelMatchPredictionBatches.Where(b => b.RunId == runId).ExecuteDeleteAsync();

            Func<Task> act = () => repository.RecordBatchResult(runId, 0, new Dictionary<int, string>());

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task GetRunIdsAwaitingFinalisationAndNotLeased_AbandonedRun_BecomesEligibleOnceAllLateResultsArrive()
        {
            var runId = await CreateRun(totalBatchCount: 1);
            await repository.TryMarkRunAsAbandoned(runId, DateTime.UtcNow); // run + batch Abandoned

            // Still has an Abandoned batch, so not yet finalisable.
            (await repository.GetRunIdsAwaitingFinalisationAndNotLeased()).Should().NotContain(runId);

            await repository.RecordBatchResult(runId, 0, new Dictionary<int, string>()); // late result arrives

            (await repository.GetRunIdsAwaitingFinalisationAndNotLeased()).Should().Contain(runId);
        }

        [Test]
        public async Task CleanupBatchesForRunsAbandonedBefore_DeletesBatchesAndMarksRunCleanedUp()
        {
            var runId = await CreateRun(totalBatchCount: 2);
            await repository.TryMarkRunAsAbandoned(runId, DateTime.UtcNow);

            var deleted = await repository.CleanupBatchesForRunsAbandonedBefore(DateTime.UtcNow.AddMinutes(1));

            deleted.Should().Be(2);
            var run = (await repository.GetRunWithResults(runId)).Run;
            run.Status.Should().Be(ParallelMatchPredictionRunStatus.AbandonedAndCleanedUp);
            run.Batches.Should().BeEmpty();
        }

        private async Task<int> CreateRun(int totalBatchCount)
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
