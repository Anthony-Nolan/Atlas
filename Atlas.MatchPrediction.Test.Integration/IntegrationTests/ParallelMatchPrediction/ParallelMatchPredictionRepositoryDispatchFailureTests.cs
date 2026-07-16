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

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.ParallelMatchPrediction;

[TestFixture]
public class ParallelMatchPredictionRepositoryDispatchFailureTests
{
    private MatchPredictionContext context;
    private ParallelMatchPredictionRepository repository;

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
    public async Task MarkRunAsDispatchFailed_SuccessPath()
    {
        var runId = (await CreateRun(totalBatchCount: 3)).RunId;
        var failureMessage = fixture.Create<string>();
        var failureException = fixture.Create<string>();
        var nowUtc = DateTime.UtcNow;

        await repository.MarkRunAsDispatchFailed(runId, failureMessage, failureException, nowUtc);

        var batches = await GetBatches(runId);
        batches.Should().HaveCount(3);
        batches.Should().AllSatisfy(batch =>
        {
            batch.BatchStatus.Should().Be(ParallelMatchPredictionBatchStatus.Failed);
            batch.FailureMessage.Should().Be(failureMessage);
            batch.FailureException.Should().Be(failureException);
            batch.ResultReceivedTimeUtc.Should().BeCloseTo(nowUtc, TimeSpan.FromSeconds(1));
        });

        // The run must stay Running so the finaliser picks it up (all batches now Failed) and runs the failure
        // pipeline, which is what transitions it to FailedDuringBatchProcessing.
        var run = (await repository.GetRunWithResults(runId)).Run;
        run.IsSuccessful.Should().BeFalse();
        run.Status.Should().Be(ParallelMatchPredictionRunStatus.Running);
    }

    [Test]
    public async Task MarkRunAsDispatchFailed_RunBecomesFinalisationEligible_AndIsNotSelectedForAbandonment()
    {
        var runId = (await CreateRun(totalBatchCount: 2)).RunId;

        await repository.MarkRunAsDispatchFailed(runId, fixture.Create<string>(), fixture.Create<string>(), DateTime.UtcNow);

        // The finaliser owns the downstream failure processing, so the run must be eligible on its next tick...
        (await repository.GetRunIdsAwaitingFinalisationAndNotLeased()).Should().Contain(runId);
        // ...and the abandonment sweep must not double-report it (no batch is left Requested).
        (await repository.GetRunIdsToAbandon(DateTime.UtcNow.AddMinutes(1))).Should().NotContain(runId);
    }

    [Test]
    public async Task MarkRunAsDispatchFailed_CalledTwice_DoesOverwriteTheFirstFailureDetail()
    {
        var runId = (await CreateRun(totalBatchCount: 1)).RunId;
        var firstMessage = fixture.Create<string>();
        var secondMessage = fixture.Create<string>();
        await repository.MarkRunAsDispatchFailed(runId, firstMessage, fixture.Create<string>(), DateTime.UtcNow);

        await repository.MarkRunAsDispatchFailed(runId, secondMessage, fixture.Create<string>(), DateTime.UtcNow);

        var run = (await repository.GetRunWithResults(runId)).Run;
        run.IsSuccessful.Should().BeFalse();
        run.Status.Should().Be(ParallelMatchPredictionRunStatus.Running);
        (await GetBatches(runId)).Should().ContainSingle()
            .Which.FailureMessage.Should().Be(secondMessage);
    }

    [Test]
    public async Task MarkRunAsDispatchFailed_TruncatesFailureMessageToColumnLimit()
    {
        var runId = (await CreateRun(totalBatchCount: 1)).RunId;
        var overlongMessage = string.Join(string.Empty, fixture.CreateMany<char>(ParallelMatchPredictionBatch.FailureMessageMaxLength * 2));

        await repository.MarkRunAsDispatchFailed(runId, overlongMessage, fixture.Create<string>(), DateTime.UtcNow);

        (await GetBatches(runId)).Should().ContainSingle()
            .Which.FailureMessage.Should().Be(overlongMessage[..ParallelMatchPredictionBatch.FailureMessageMaxLength]);
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

    private async Task<List<ParallelMatchPredictionBatch>> GetBatches(int runId)
    {
        return await context.ParallelMatchPredictionBatches
            .AsNoTracking()
            .Where(b => b.RunId == runId)
            .OrderBy(b => b.BatchSequenceNumber)
            .ToListAsync();
    }
}