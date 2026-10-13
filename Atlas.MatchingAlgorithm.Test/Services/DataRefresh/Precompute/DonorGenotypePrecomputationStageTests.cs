using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Repositories.Precompute;
using Atlas.MatchingAlgorithm.Exceptions;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Notifications;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchingAlgorithm.Test.TestHelpers;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.DataRefresh;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using BatchStatus = Atlas.MatchingAlgorithm.Data.Models.Entities.DonorGenotypePrecomputationBatchStatus;
using RunStatus = Atlas.MatchingAlgorithm.Data.Models.Entities.DonorGenotypePrecomputationRunStatus;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh.Precompute;

[TestFixture]
public class DonorGenotypePrecomputationStageTests
{
    private const TransientDatabase DormantDatabase = TransientDatabase.DatabaseB;
    private const int PollIntervalSeconds = 60;
    private const int StallAlertMinutes = 60;

    /// <summary>The polls of a wait one stall alert time long.</summary>
    private const int PollsOfOneStallAlertTime = StallAlertMinutes * 60 / PollIntervalSeconds;

    /// <summary>The polls of a wait two stall alert times long.</summary>
    private const int PollsOfTwoStallAlertTimes = 2 * PollsOfOneStallAlertTime;

    private Fixture fixture;
    private IDonorGenotypePrecomputationRepository repository;
    private IActiveDatabaseProvider activeDatabaseProvider;
    private IDonorGenotypePrecomputationBatchDispatcher dispatcher;
    private IDataRefreshSupportNotificationSender notificationSender;
    private DonorGenotypePrecomputationSettings settings;
    private DataRefreshRecord record;
    private int runId;

    private DonorGenotypePrecomputationStage stage;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();

        repository = Substitute.For<IDonorGenotypePrecomputationRepository>();
        var repositoryFactory = Substitute.For<IDormantRepositoryFactory>();
        repositoryFactory.GetDonorGenotypePrecomputationRepository().Returns(repository);

        activeDatabaseProvider = Substitute.For<IActiveDatabaseProvider>();
        activeDatabaseProvider.GetDormantDatabase().Returns(DormantDatabase);

        dispatcher = Substitute.For<IDonorGenotypePrecomputationBatchDispatcher>();
        notificationSender = Substitute.For<IDataRefreshSupportNotificationSender>();

        settings = fixture.Build<DonorGenotypePrecomputationSettings>()
            .With(s => s.PollIntervalSeconds, PollIntervalSeconds)
            .With(s => s.StallAlertMinutes, StallAlertMinutes)
            .With(s => s.MaxFailedDonorFraction, 1.0 / fixture.Create<int>())
            .Create();

        record = DataRefreshRecordBuilder.New.WithDatabase(DormantDatabase).Build();
        runId = fixture.Create<int>();

        // A stage that gets to its report finds staging data, unless a test says otherwise.
        repository.HasStagingData().Returns(true);

        stage = new DonorGenotypePrecomputationStage(
            repositoryFactory,
            activeDatabaseProvider,
            dispatcher,
            notificationSender,
            settings,
            new InstantTimeProvider(fixture.Create<DateTimeOffset>()),
            NullLogger<DonorGenotypePrecomputationStage>.Instance);
    }

    #region Checks before any work

    private static IEnumerable<TestCaseData> SettingsOutOfRange()
    {
        yield return Case(s => s.GroupsPerBatch = 0, "GroupsPerBatch 0");
        yield return Case(s => s.PollIntervalSeconds = 0, "PollIntervalSeconds 0");
        yield return Case(s => s.StallAlertMinutes = 0, "StallAlertMinutes 0");
        yield return Case(s => s.MaxFailedDonorFraction = -0.5, "MaxFailedDonorFraction below 0");
        yield return Case(s => s.MaxFailedDonorFraction = 1.5, "MaxFailedDonorFraction above 1");
        yield return Case(s => s.MaxFailedDonorFraction = double.NaN, "MaxFailedDonorFraction not a number");

        static TestCaseData Case(Action<DonorGenotypePrecomputationSettings> change, string name) =>
            new TestCaseData(change).SetArgDisplayNames(name);
    }

    [TestCaseSource(nameof(SettingsOutOfRange))]
    public async Task Run_WithASettingOutOfRange_ThrowsBeforeAnyDatabaseCall(Action<DonorGenotypePrecomputationSettings> change)
    {
        change(settings);

        var act = () => stage.Run(record, DataRefreshStageExecutionMode.FromScratch, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidDataRefreshConfigurationException>();
        repository.ReceivedCalls().Should().BeEmpty();
    }

    [Test]
    public async Task Run_WhenTheRecordDoesNotNameTheDormantDatabase_ThrowsBeforeAnyDatabaseCall()
    {
        activeDatabaseProvider.GetDormantDatabase().Returns(TransientDatabase.DatabaseA);

        var act = () => stage.Run(record, DataRefreshStageExecutionMode.FromScratch, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        repository.ReceivedCalls().Should().BeEmpty();
    }

    #endregion

    #region The state of the run

    [Test]
    public async Task Run_WhenTheRecordHasNoRun_BuildsARunWithTheVersionOfTheRecordAndTheGroupsPerBatch()
    {
        DonorGenotypePrecomputationRun noRun = null;
        repository.GetRun(record.Id).Returns(noRun, RunWithStatus(RunStatus.Running), RunWithStatus(RunStatus.Completed));
        GivenABuild();
        using var cancellation = new CancellationTokenSource();

        await stage.Run(record, DataRefreshStageExecutionMode.FromScratch, cancellation.Token);

        await repository.Received(1).StartBuild(record.Id, record.HlaNomenclatureVersion, settings.GroupsPerBatch);
        await repository.Received(1).BuildRun(runId, cancellation.Token);
    }

    [Test]
    public async Task Run_WhenTheRunIsBuilding_BuildsItAgain()
    {
        repository.GetRun(record.Id).Returns(RunWithStatus(RunStatus.Building), RunWithStatus(RunStatus.Completed));
        GivenABuild();

        await stage.Run(record, DataRefreshStageExecutionMode.Continuation, CancellationToken.None);

        await repository.Received(1).StartBuild(record.Id, record.HlaNomenclatureVersion, settings.GroupsPerBatch);
        await repository.Received(1).BuildRun(runId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Run_WhenTheBuildMakesNoBatches_NeitherDispatchesNorWaits()
    {
        DonorGenotypePrecomputationRun noRun = null;
        repository.GetRun(record.Id).Returns(noRun, RunWithStatus(RunStatus.Completed));
        GivenABuild();
        repository.HasStagingData().Returns(false);

        await stage.Run(record, DataRefreshStageExecutionMode.FromScratch, CancellationToken.None);

        await dispatcher.DidNotReceiveWithAnyArgs().DispatchPendingBatches(default, default);
        await repository.DidNotReceiveWithAnyArgs().GetBatchCounts(default);
    }

    [Test]
    public async Task Run_WhenTheRunIsRunning_DoesNotBuild_AndDispatchesEveryPendingBatchOfTheRun()
    {
        GivenTheRunIsCompleteAfterPolls(fixture.Create<int>());

        await stage.Run(record, DataRefreshStageExecutionMode.Continuation, CancellationToken.None);

        await repository.DidNotReceiveWithAnyArgs().StartBuild(default, default, default);
        await repository.DidNotReceiveWithAnyArgs().BuildRun(default, default);
        await dispatcher.Received(1).DispatchPendingBatches(
            new DonorGenotypePrecomputationRunLocation(record.Id, DormantDatabase, runId),
            PendingBatchSelection.All);
    }

    [TestCase(RunStatus.Completed)]
    [TestCase(RunStatus.CompletedWithFailures)]
    public async Task Run_WhenTheRunIsComplete_NeitherBuildsNorDispatches(RunStatus status)
    {
        repository.GetRun(record.Id).Returns(RunWithStatus(status));
        repository.GetFailureSummary(runId).Returns(FailureSummary());

        await stage.Run(record, DataRefreshStageExecutionMode.Continuation, CancellationToken.None);

        await repository.DidNotReceiveWithAnyArgs().StartBuild(default, default, default);
        await dispatcher.DidNotReceiveWithAnyArgs().DispatchPendingBatches(default, default);
    }

    [Test]
    public async Task Run_WhenTheRunIsCancelled_ThrowsAndNeitherBuildsNorDispatches()
    {
        repository.GetRun(record.Id).Returns(RunWithStatus(RunStatus.Cancelled));

        var act = () => stage.Run(record, DataRefreshStageExecutionMode.Continuation, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await repository.DidNotReceiveWithAnyArgs().StartBuild(default, default, default);
        await dispatcher.DidNotReceiveWithAnyArgs().DispatchPendingBatches(default, default);
    }

    #endregion

    #region The wait

    [Test]
    public async Task Run_WaitsUntilTheRunIsComplete()
    {
        var pollCount = fixture.Create<int>();
        GivenTheRunIsCompleteAfterPolls(pollCount);

        await stage.Run(record, DataRefreshStageExecutionMode.Continuation, CancellationToken.None);

        await repository.Received(pollCount).GetBatchCounts(runId);
        await repository.Received(1).TruncateStagingTables();
    }

    [TestCase(RunStatus.Cancelled)]
    [TestCase(RunStatus.Building)]
    public async Task Run_WhenTheRunStopsRunningDuringTheWait_ThrowsAndReportsNothing(RunStatus status)
    {
        GivenTheRunIsCompleteAfterPolls(fixture.Create<int>(), finalStatus: status);

        var act = () => stage.Run(record, DataRefreshStageExecutionMode.Continuation, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await repository.DidNotReceive().TruncateStagingTables();
    }

    [Test]
    public async Task Run_WhenTheLeaseIsLostDuringTheWait_StopsAndReportsNothing()
    {
        // Not disposed: the callback below uses it, and a source with no timer holds nothing to release.
        var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        repository.GetRun(record.Id).Returns(RunWithStatus(RunStatus.Running));
        repository.GetBatchCounts(runId).Returns(CountsWithTerminalBatches(fixture.Create<int>()));
        dispatcher.DispatchPendingBatches(default, default).ReturnsForAnyArgs(_ =>
        {
            cancellation.Cancel();
            return fixture.Create<int>();
        });

        var act = () => stage.Run(record, DataRefreshStageExecutionMode.Continuation, token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        await repository.DidNotReceive().HasStagingData();
    }

    [Test]
    public async Task Run_WhenNoBatchFinishesForTheStallAlertTime_SendsOneStallAlert()
    {
        GivenTheRunIsCompleteAfterPolls(PollsOfTwoStallAlertTimes, terminalBatchesAtPoll: _ => 0);

        await stage.Run(record, DataRefreshStageExecutionMode.Continuation, CancellationToken.None);

        await notificationSender.Received(1).SendPrecomputationStallAlert(
            record.Id,
            runId,
            Arg.Is<TimeSpan>(timeWithoutProgress => timeWithoutProgress >= TimeSpan.FromMinutes(StallAlertMinutes)),
            Arg.Any<DonorGenotypePrecomputationBatchCounts>());
    }

    [Test]
    public async Task Run_WhenBatchesFinishAgainAfterAStall_SendsAnotherAlertForTheNextStall()
    {
        // Two stalls, each two stall alert times long, with one batch finished between them.
        GivenTheRunIsCompleteAfterPolls(
            2 * PollsOfTwoStallAlertTimes,
            terminalBatchesAtPoll: poll => poll <= PollsOfTwoStallAlertTimes ? 0 : 1);

        await stage.Run(record, DataRefreshStageExecutionMode.Continuation, CancellationToken.None);

        await notificationSender.ReceivedWithAnyArgs(2).SendPrecomputationStallAlert(default, default, default, default);
    }

    [Test]
    public async Task Run_WhenBatchesKeepFinishing_SendsNoStallAlert()
    {
        GivenTheRunIsCompleteAfterPolls(PollsOfTwoStallAlertTimes);

        await stage.Run(record, DataRefreshStageExecutionMode.Continuation, CancellationToken.None);

        await notificationSender.DidNotReceiveWithAnyArgs().SendPrecomputationStallAlert(default, default, default, default);
    }

    // A SqlException has no public constructor. A TimeoutException is a temporary error too.
    [Test]
    public async Task Run_WhenAPollFailsWithATemporaryError_ReadsTheRunAgainAtTheNextPoll()
    {
        repository.GetRun(record.Id).Returns(
            _ => RunWithStatus(RunStatus.Running),
            _ => throw new TimeoutException(fixture.Create<string>()),
            _ => RunWithStatus(RunStatus.Completed));

        var act = () => stage.Run(record, DataRefreshStageExecutionMode.Continuation, CancellationToken.None);

        await act.Should().NotThrowAsync();
        await repository.Received(3).GetRun(record.Id);
        await repository.Received(1).TruncateStagingTables();
    }

    [Test]
    public async Task Run_WhenThePollsFailWithTemporaryErrorsForTheStallAlertTime_Throws()
    {
        repository.GetRun(record.Id).Returns(
            _ => RunWithStatus(RunStatus.Running),
            _ => throw new TimeoutException(fixture.Create<string>()));

        var act = () => stage.Run(record, DataRefreshStageExecutionMode.Continuation, CancellationToken.None);

        await act.Should().ThrowAsync<TimeoutException>();
        // The read at the start of the stage, and a poll at each interval from the first failure to the stall alert time.
        await repository.Received(PollsOfOneStallAlertTime + 2).GetRun(record.Id);
        await repository.DidNotReceive().TruncateStagingTables();
    }

    [Test]
    public async Task Run_WhenAPollReadsTheRunBetweenFailedPolls_CountsTheTimeOfTheFailedPollsAgainFromTheNextFailure()
    {
        // Two series of failed polls, each just shorter than the stall alert time, with one poll that reads the run between them.
        var calls = 0;
        repository.GetRun(record.Id).Returns(_ =>
        {
            calls++;
            return calls switch
            {
                1 => RunWithStatus(RunStatus.Running),
                <= 1 + PollsOfOneStallAlertTime => throw new TimeoutException(fixture.Create<string>()),
                2 + PollsOfOneStallAlertTime => RunWithStatus(RunStatus.Running),
                <= 2 + 2 * PollsOfOneStallAlertTime => throw new TimeoutException(fixture.Create<string>()),
                _ => RunWithStatus(RunStatus.Completed)
            };
        });
        repository.GetBatchCounts(runId).Returns(CountsWithTerminalBatches(fixture.Create<int>()));

        var act = () => stage.Run(record, DataRefreshStageExecutionMode.Continuation, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task Run_WhenAPollFailsWithAnErrorThatIsNotTemporary_ThrowsAtOnce()
    {
        repository.GetRun(record.Id).Returns(
            _ => RunWithStatus(RunStatus.Running),
            _ => throw new ArgumentException(fixture.Create<string>()));

        var act = () => stage.Run(record, DataRefreshStageExecutionMode.Continuation, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        await repository.Received(2).GetRun(record.Id);
    }

    #endregion

    #region The report

    [Test]
    public async Task Run_WhenTheRunIsCompleteWithNoFailure_SendsNoAlertAndRemovesTheStagingData()
    {
        repository.GetRun(record.Id).Returns(RunWithStatus(RunStatus.Completed));

        await stage.Run(record, DataRefreshStageExecutionMode.Continuation, CancellationToken.None);

        await repository.DidNotReceiveWithAnyArgs().GetFailureSummary(default);
        await notificationSender.DidNotReceiveWithAnyArgs().SendPrecomputationFailureSummary(default, default, default, default);
        await repository.Received(1).TruncateStagingTables();
    }

    [Test]
    public async Task Run_WhenTheRunIsCompleteWithFailures_SendsTheFailureSummaryBeforeItRemovesTheStagingData()
    {
        var summary = FailureSummary();
        repository.GetRun(record.Id).Returns(RunWithStatus(RunStatus.CompletedWithFailures));
        repository.GetFailureSummary(runId).Returns(summary);

        await stage.Run(record, DataRefreshStageExecutionMode.Continuation, CancellationToken.None);

        Received.InOrder(() =>
        {
            notificationSender.SendPrecomputationFailureSummary(record.Id, runId, summary, settings.MaxFailedDonorFraction);
            repository.TruncateStagingTables();
        });
    }

    [Test]
    public async Task Run_WhenMoreDonorsFailedThanTheThresholdAllows_SendsTheSummaryAndCompletes()
    {
        settings.MaxFailedDonorFraction = 0;
        var summary = FailureSummary();
        repository.GetRun(record.Id).Returns(RunWithStatus(RunStatus.CompletedWithFailures));
        repository.GetFailureSummary(runId).Returns(summary);

        var act = () => stage.Run(record, DataRefreshStageExecutionMode.Continuation, CancellationToken.None);

        await act.Should().NotThrowAsync();
        await notificationSender.Received(1).SendPrecomputationFailureSummary(record.Id, runId, summary, 0);
        await repository.Received(1).TruncateStagingTables();
    }

    [TestCase(RunStatus.Completed)]
    [TestCase(RunStatus.CompletedWithFailures)]
    public async Task Run_WhenTheStagingDataIsGone_ReportsNothingAndRemovesNothing(RunStatus status)
    {
        repository.GetRun(record.Id).Returns(RunWithStatus(status));
        repository.HasStagingData().Returns(false);

        await stage.Run(record, DataRefreshStageExecutionMode.Continuation, CancellationToken.None);

        await repository.DidNotReceiveWithAnyArgs().GetFailureSummary(default);
        await notificationSender.DidNotReceiveWithAnyArgs().SendPrecomputationFailureSummary(default, default, default, default);
        await repository.DidNotReceive().TruncateStagingTables();
    }

    #endregion

    private DonorGenotypePrecomputationRun RunWithStatus(RunStatus status) =>
        fixture.Build<DonorGenotypePrecomputationRun>()
            .With(run => run.Id, runId)
            .With(run => run.DataRefreshRecordId, record.Id)
            .With(run => run.Status, status)
            .Create();

    private void GivenABuild()
    {
        repository.StartBuild(default, default, default).ReturnsForAnyArgs(RunWithStatus(RunStatus.Building));
        repository.BuildRun(default, default).ReturnsForAnyArgs(fixture.Create<DonorGenotypePrecomputationBuildResult>());
    }

    /// <summary>
    /// The run is running at the start of the stage and for <paramref name="pollCount"/> polls of the wait, then it is
    /// <paramref name="finalStatus"/>. By default, one more batch is done at each poll.
    /// </summary>
    private void GivenTheRunIsCompleteAfterPolls(
        int pollCount,
        Func<int, int> terminalBatchesAtPoll = null,
        RunStatus finalStatus = RunStatus.Completed)
    {
        var terminalBatches = terminalBatchesAtPoll ?? (poll => poll);
        var polls = 0;
        repository.GetBatchCounts(runId).Returns(_ => CountsWithTerminalBatches(terminalBatches(++polls)));
        repository.GetRun(record.Id).Returns(_ => RunWithStatus(polls < pollCount ? RunStatus.Running : finalStatus));
    }

    private static DonorGenotypePrecomputationBatchCounts CountsWithTerminalBatches(int terminalBatchCount) =>
        new(new Dictionary<BatchStatus, int> { [BatchStatus.ResultsReceived] = terminalBatchCount, [BatchStatus.Requested] = 1 }, 0);

    private DonorGenotypePrecomputationFailureSummary FailureSummary() =>
        new(
            fixture.Create<int>(),
            fixture.Create<int>(),
            fixture.Create<int>(),
            fixture.Create<int>(),
            fixture.CreateMany<DonorGenotypePrecomputationFailureSample>().ToList());
}
