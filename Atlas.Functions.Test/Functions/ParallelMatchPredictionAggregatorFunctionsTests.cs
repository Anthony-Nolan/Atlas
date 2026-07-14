using Atlas.Functions.Functions;
using Atlas.Functions.Services;
using Atlas.Functions.Settings;
using Atlas.MatchPrediction.Data.Repositories;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.Functions.Test.Functions;

[TestFixture]
internal class ParallelMatchPredictionAggregatorFunctionsTests
{
    private IParallelMatchPredictionRepository repository;
    private IParallelMatchPredictionCompletionService completionService;
    private ParallelMatchPredictionAggregatorFunctions functions;

    private Fixture fixture;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        repository = Substitute.For<IParallelMatchPredictionRepository>();
        completionService = Substitute.For<IParallelMatchPredictionCompletionService>();

        functions = new ParallelMatchPredictionAggregatorFunctions(
            repository,
            completionService,
            Options.Create(new OrchestrationSettings { AbandonBatchAfterMinutes = 60, ParallelBatchRetentionDays = 90 }),
            Substitute.For<ILogger<ParallelMatchPredictionAggregatorFunctions>>()
        );
    }

    [Test]
    public async Task MarkRunsAsAbandoned_AbandonsEachRunReturnedByTheRepository()
    {
        var runIds = fixture.CreateMany<int>(3).ToList();
        repository.GetRunIdsToAbandon(Arg.Any<DateTime>()).Returns(runIds);

        await functions.MarkRunsAsAbandoned(null);

        foreach (var runId in runIds)
        {
            await completionService.Received(1).AbandonRun(runId);
        }
    }

    [Test]
    public async Task MarkRunsAsAbandoned_WhenAbandoningOneRunThrows_StillAbandonsTheOthersThenThrows()
    {
        var runIds = fixture.CreateMany<int>(3).ToList();
        repository.GetRunIdsToAbandon(Arg.Any<DateTime>()).Returns(runIds);
        completionService.AbandonRun(runIds[1]).Returns(Task.FromException(new InvalidOperationException(fixture.Create<string>())));

        // A single failure does not block the other runs, but the invocation is still reported as failed so the
        // partial failure surfaces in monitoring rather than being silently swallowed.
        var thrown = await functions.Invoking(f => f.MarkRunsAsAbandoned(null))
            .Should().ThrowAsync<AggregateException>();
        thrown.Which.InnerExceptions.Should().HaveCount(1);

        await completionService.Received(1).AbandonRun(runIds[0]);
        await completionService.Received(1).AbandonRun(runIds[2]);
    }

    [Test]
    public async Task MarkRunsAsAbandoned_WhenNoRunsToAbandon_DoesNotCallCompletionService()
    {
        repository.GetRunIdsToAbandon(Arg.Any<DateTime>()).Returns(new List<int>());

        await functions.MarkRunsAsAbandoned(null);

        await completionService.DidNotReceiveWithAnyArgs().AbandonRun(default);
    }

    [Test]
    public async Task FinaliseCompletedParallelMatchPredictionRuns_FinalisesEachClaimedRunReturnedByTheRepository()
    {
        var runIds = fixture.CreateMany<int>(3).ToList();
        repository.GetRunIdsAwaitingFinalisationAndNotLeased().Returns(runIds);
        repository.TryClaimFinalisationLease(Arg.Any<int>(), Arg.Any<Guid>()).Returns(true);

        await functions.FinaliseCompletedParallelMatchPredictionRuns(null);

        foreach (var runId in runIds)
        {
            await completionService.Received(1).FinaliseRun(runId);
        }
    }

    [Test]
    public async Task FinaliseCompletedParallelMatchPredictionRuns_WhenFinalisingOneRunThrows_StillFinalisesTheOthersThenThrows()
    {
        var runIds = fixture.CreateMany<int>(3).ToList();
        repository.GetRunIdsAwaitingFinalisationAndNotLeased().Returns(runIds);
        repository.TryClaimFinalisationLease(Arg.Any<int>(), Arg.Any<Guid>()).Returns(true);
        completionService.FinaliseRun(runIds[1]).Returns(Task.FromException(new InvalidOperationException(fixture.Create<string>())));

        // A single failure does not block the other runs, but the invocation is still reported as failed so the
        // partial failure surfaces in monitoring rather than being silently swallowed.
        var thrown = await functions.Invoking(f => f.FinaliseCompletedParallelMatchPredictionRuns(null))
            .Should().ThrowAsync<AggregateException>();
        thrown.Which.InnerExceptions.Should().HaveCount(1);

        await completionService.Received(1).FinaliseRun(runIds[0]);
        await completionService.Received(1).FinaliseRun(runIds[2]);
    }

    [Test]
    public async Task FinaliseCompletedParallelMatchPredictionRuns_WhenRunAlreadyClaimedByAnotherInvocation_DoesNotFinaliseIt()
    {
        var claimedRunId = fixture.Create<int>();
        var alreadyLeasedRunId = fixture.Create<int>();
        repository.GetRunIdsAwaitingFinalisationAndNotLeased()
            .Returns(new List<int> { claimedRunId, alreadyLeasedRunId });
        repository.TryClaimFinalisationLease(claimedRunId, Arg.Any<Guid>()).Returns(true);
        repository.TryClaimFinalisationLease(alreadyLeasedRunId, Arg.Any<Guid>()).Returns(false);

        await functions.FinaliseCompletedParallelMatchPredictionRuns(null);

        await completionService.Received(1).FinaliseRun(claimedRunId);
        await completionService.DidNotReceive().FinaliseRun(alreadyLeasedRunId);
    }

    [Test]
    public async Task FinaliseCompletedParallelMatchPredictionRuns_WhenNoRunsAwaitingFinalisation_DoesNotCallCompletionService()
    {
        repository.GetRunIdsAwaitingFinalisationAndNotLeased().Returns(new List<int>());

        await functions.FinaliseCompletedParallelMatchPredictionRuns(null);

        await completionService.DidNotReceiveWithAnyArgs().FinaliseRun(default);
    }

    [Test]
    public async Task CleanupOldParallelMatchPredictionBatches_CleansRunsRegardlessOfStatus()
    {
        await functions.CleanupOldParallelMatchPredictionBatches(null);

        // A single status-agnostic clean-up (keyed off initiation time) purges finalised, failed and
        // abandoned runs alike, marking each IsCleanedUp=true.
        await repository.Received(1).CleanupBatchesForRunsInitiatedBefore(Arg.Any<DateTime>());
    }
}