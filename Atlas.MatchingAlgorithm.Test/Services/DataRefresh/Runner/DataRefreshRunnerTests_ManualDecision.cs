using System;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Exceptions.Azure;
using Atlas.MatchingAlgorithm.Models.AzureManagement;
using Atlas.MatchingAlgorithm.Services.DataRefresh;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.DataRefresh;
using AutoFixture;
using AwesomeAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh.Runner;

[TestFixture]
public partial class DataRefreshRunnerTests
{
    // These tests are extensions of the Test setup defined in DataRefreshRunnerTests_Core: the pause for a manual decision,
    // and the refresh that resumes after the decision.

    private const AzureDatabaseSize DormantSize = AzureDatabaseSize.S0;
    private const AzureDatabaseSize ActiveSize = AzureDatabaseSize.S4;
    private const AzureDatabaseSize RefreshSize = AzureDatabaseSize.P15;

    #region The pause

    [Test]
    public async Task RefreshData_WhenThePrecomputationAwaitsAManualDecision_StartsTheScaleDownThenAlertsThenWaitsForTheScaleDown()
    {
        var autoPause = fixture.Create<int>();
        dataRefreshRunner = BuildDataRefreshRunner(DataRefreshSettingsBuilder.New
            .With(s => s.DormantDatabaseSize, DormantSize.ToString())
            .With(s => s.DormantDatabaseAutoPauseTimeout, autoPause)
            .Build());
        var record = GivenARecord();
        var report = GivenThePrecomputationAwaitsAManualDecision();
        var scaleDown = GivenTheScaleDownStarts();

        var result = await dataRefreshRunner.RefreshData(record.Id);

        result.Should().Be(DataRefreshRunResult.Paused(report));
        Received.InOrder(() =>
        {
            azureDatabaseManager.StartDatabaseScaling(Arg.Any<string>(), DormantSize, autoPause);
            dataRefreshNotificationSender.SendPrecomputationPausedAlert(record.Id, report, true);
            azureDatabaseManager.WaitForDatabaseScaling(scaleDown);
        });
    }

    [Test]
    public async Task RefreshData_WhenThePrecomputationAwaitsAManualDecision_DoesNotCompleteTheStageOrRunTheLaterStages()
    {
        GivenARecord();
        GivenThePrecomputationAwaitsAManualDecision();
        GivenTheScaleDownStarts();

        await dataRefreshRunner.RefreshData(default);

        await dataRefreshHistoryRepository.DidNotReceive()
            .MarkStageAsComplete(Arg.Any<DataRefreshRecord>(), DataRefreshStage.DonorGenotypePrecomputation);
        await dataRefreshHistoryRepository.DidNotReceive()
            .MarkStageAsComplete(Arg.Any<DataRefreshRecord>(), DataRefreshStage.DatabaseScalingTearDown);
        await azureDatabaseManager.DidNotReceive().UpdateDatabaseSize(Arg.Any<string>(), ActiveSize, Arg.Any<int?>());
        await donorUpdateProcessor.DidNotReceiveWithAnyArgs().ApplyDifferentialDonorUpdatesDuringRefresh(default, default);
    }

    [Test]
    public async Task RefreshData_WhenThePrecomputationAwaitsAManualDecision_DoesNotPauseTheRecordItself()
    {
        GivenARecord();
        GivenThePrecomputationAwaitsAManualDecision();
        GivenTheScaleDownStarts();

        await dataRefreshRunner.RefreshData(default);

        await dataRefreshHistoryRepository.DidNotReceiveWithAnyArgs().TryPauseAndReleaseLease(default, default, default);
        await dataRefreshHistoryRepository.DidNotReceiveWithAnyArgs().ClearPendingManualDecision(default);
        await dataRefreshHistoryRepository.DidNotReceiveWithAnyArgs().ApplyManualCompleteDecision(default);
    }

    [Test]
    public async Task RefreshData_WhenAzureRefusesToStartTheScaleDown_SendsTheTeardownAlert_AndStillPauses()
    {
        var record = GivenARecord();
        var report = GivenThePrecomputationAwaitsAManualDecision();
        azureDatabaseManager.StartDatabaseScaling(default, default, default)
            .ThrowsAsyncForAnyArgs(new AzureManagementException(fixture.Create<string>()));

        var result = await dataRefreshRunner.RefreshData(record.Id);

        result.Should().Be(DataRefreshRunResult.Paused(report));
        await dataRefreshNotificationSender.Received(1).SendTeardownFailureAlert(record.Id);
        await dataRefreshNotificationSender.Received(1).SendPrecomputationPausedAlert(record.Id, report, false);
        await azureDatabaseManager.DidNotReceiveWithAnyArgs().WaitForDatabaseScaling(default);
    }

    [Test]
    public async Task RefreshData_WhenTheScaleDownFailsDuringTheWait_SendsTheTeardownAlert_AndStillPauses()
    {
        var record = GivenARecord();
        var report = GivenThePrecomputationAwaitsAManualDecision();
        GivenTheScaleDownStarts();
        azureDatabaseManager.WaitForDatabaseScaling(default).ThrowsAsyncForAnyArgs(new AzureManagementException(fixture.Create<string>()));

        var result = await dataRefreshRunner.RefreshData(record.Id);

        result.Should().Be(DataRefreshRunResult.Paused(report));
        Received.InOrder(() =>
        {
            dataRefreshNotificationSender.SendPrecomputationPausedAlert(record.Id, report, true);
            dataRefreshNotificationSender.SendTeardownFailureAlert(record.Id);
        });
    }

    [Test]
    public async Task RefreshData_WhenTheLeaseIsLostDuringThePause_Throws()
    {
        GivenARecord();
        GivenThePrecomputationAwaitsAManualDecision();
        GivenTheScaleDownStarts();
        using var cancellation = new CancellationTokenSource();
        azureDatabaseManager.WaitForDatabaseScaling(default).ReturnsForAnyArgs(_ => cancellation.CancelAsync());

        var act = () => dataRefreshRunner.RefreshData(default, cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region The resumed refresh

    [Test]
    public async Task RefreshData_WithAPendingCompleteDecision_MarksThePrecomputationCompleteBeforeAnyStage_AndRunsTheLaterStagesWithNoScaleUp()
    {
        dataRefreshRunner = BuildDataRefreshRunner(DataRefreshSettingsBuilder.New
            .With(s => s.ActiveDatabaseSize, ActiveSize.ToString())
            .With(s => s.RefreshDatabaseSize, RefreshSize.ToString())
            .Build());
        var record = GivenAPausedRecordWithAPendingDecision(DataRefreshManualDecision.Complete);

        var result = await dataRefreshRunner.RefreshData(record.Id);

        result.IsPaused.Should().BeFalse();
        Received.InOrder(() =>
        {
            dataRefreshHistoryRepository.ApplyManualCompleteDecision(record);
            azureDatabaseManager.UpdateDatabaseSize(Arg.Any<string>(), ActiveSize, Arg.Any<int?>());
            donorUpdateProcessor.ApplyDifferentialDonorUpdatesDuringRefresh(Arg.Any<TransientDatabase>(), Arg.Any<string>());
        });
        await azureDatabaseManager.DidNotReceive().UpdateDatabaseSize(Arg.Any<string>(), RefreshSize, Arg.Any<int?>());
        await donorGenotypePrecomputationStage.DidNotReceiveWithAnyArgs().Run(default, default, default);
    }

    [Test]
    public async Task RefreshData_WithAPendingCompleteDecision_SendsTheResumedEvent()
    {
        var record = GivenAPausedRecordWithAPendingDecision(DataRefreshManualDecision.Complete);

        await dataRefreshRunner.RefreshData(record.Id);

        manualDecisionEvents.Received(1).Resumed(record.Id, DataRefreshManualDecision.Complete);
    }

    [Test]
    public async Task RefreshData_WithAPendingRetryDecision_ScalesUpBeforeThePrecomputation_AndLeavesTheDecisionToTheStage()
    {
        dataRefreshRunner = BuildDataRefreshRunner(DataRefreshSettingsBuilder.New
            .With(s => s.RefreshDatabaseSize, RefreshSize.ToString())
            .Build());
        var record = GivenAPausedRecordWithAPendingDecision(DataRefreshManualDecision.RetryFailedBatches);

        await dataRefreshRunner.RefreshData(record.Id);

        manualDecisionEvents.Received(1).Resumed(record.Id, DataRefreshManualDecision.RetryFailedBatches);
        Received.InOrder(() =>
        {
            azureDatabaseManager.UpdateDatabaseSize(Arg.Any<string>(), RefreshSize, Arg.Any<int?>());
            donorGenotypePrecomputationStage.Run(record, DataRefreshStageExecutionMode.Continuation, Arg.Any<CancellationToken>());
        });
        await dataRefreshHistoryRepository.DidNotReceiveWithAnyArgs().ApplyManualCompleteDecision(default);
        await dataRefreshHistoryRepository.DidNotReceiveWithAnyArgs().ClearPendingManualDecision(default);
    }

    [Test]
    public async Task RefreshData_WithNoPendingDecision_SendsNoResumedEvent()
    {
        GivenARecord();

        await dataRefreshRunner.RefreshData(default);

        manualDecisionEvents.DidNotReceiveWithAnyArgs().Resumed(default, default);
        await dataRefreshHistoryRepository.DidNotReceiveWithAnyArgs().ApplyManualCompleteDecision(default);
    }

    #endregion

    #region The staging data of the precomputation stage

    [Test]
    public async Task RefreshData_WhenAllStagesComplete_RemovesTheStagingDataOfTheRecordAfterTheLastStage()
    {
        var record = GivenARecord();

        await dataRefreshRunner.RefreshData(record.Id);

        Received.InOrder(() =>
        {
            dataRefreshHistoryRepository.MarkStageAsComplete(record, DataRefreshStage.QueuedDonorUpdateProcessing);
            precomputationRunCanceller.RemoveStagingData(record);
        });
    }

    [Test]
    public async Task RefreshData_AfterAnAttemptThatAppliedACompleteDecision_StillRemovesTheStagingData()
    {
        // The attempt that applied the decision stopped in a later stage, so this record has no decision any more.
        var record = DataRefreshRecordBuilder.New
            .With(r => r.Id, fixture.Create<int>())
            .WithStagesCompletedUpToAndIncluding(DataRefreshStage.DonorGenotypePrecomputation)
            .Build();
        dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(record);

        await dataRefreshRunner.RefreshData(record.Id);

        await precomputationRunCanceller.Received(1).RemoveStagingData(record);
    }

    [Test]
    public async Task RefreshData_WhenThePrecomputationAwaitsAManualDecision_DoesNotRemoveTheStagingData()
    {
        GivenARecord();
        GivenThePrecomputationAwaitsAManualDecision();
        GivenTheScaleDownStarts();

        await dataRefreshRunner.RefreshData(default);

        await precomputationRunCanceller.DidNotReceiveWithAnyArgs().RemoveStagingData(default);
    }

    [Test]
    public async Task RefreshData_WhenAStageFails_DoesNotRemoveTheStagingData()
    {
        GivenARecord();
        donorUpdateProcessor.ApplyDifferentialDonorUpdatesDuringRefresh(default, default)
            .ThrowsAsyncForAnyArgs(new Exception(fixture.Create<string>()));

        var act = () => dataRefreshRunner.RefreshData(default);

        await act.Should().ThrowAsync<Exception>();
        await precomputationRunCanceller.DidNotReceiveWithAnyArgs().RemoveStagingData(default);
    }

    #endregion

    private DataRefreshRecord GivenARecord()
    {
        var record = DataRefreshRecordBuilder.New.With(r => r.Id, fixture.Create<int>()).Build();
        dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(record);
        return record;
    }

    /// <summary>
    /// A record whose stages up to the index recreation are complete, with a pending decision. The repository substitute
    /// applies a Complete decision to the record, as the real one does.
    /// </summary>
    private DataRefreshRecord GivenAPausedRecordWithAPendingDecision(DataRefreshManualDecision decision)
    {
        var record = DataRefreshRecordBuilder.New
            .With(r => r.Id, fixture.Create<int>())
            .WithStagesCompletedUpToAndIncluding(DataRefreshStage.IndexRecreation)
            .WithPendingManualDecision(decision)
            .Build();
        dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(record);
        dataRefreshHistoryRepository
            .When(r => r.ApplyManualCompleteDecision(record))
            .Do(_ =>
            {
                record.DonorGenotypePrecomputationCompleted = DateTime.UtcNow;
                record.PendingManualDecision = null;
            });
        return record;
    }

    private DonorGenotypePrecomputationFailureReport GivenThePrecomputationAwaitsAManualDecision()
    {
        var report = fixture.Create<DonorGenotypePrecomputationFailureReport>();
        donorGenotypePrecomputationStage.Run(default, default, default)
            .ReturnsForAnyArgs(DonorGenotypePrecomputationStageResult.AwaitingManualDecision(report));
        return report;
    }

    private DatabaseScalingOperation GivenTheScaleDownStarts()
    {
        var scaleDown = fixture.Create<DatabaseScalingOperation>();
        azureDatabaseManager.StartDatabaseScaling(default, default, default).ReturnsForAnyArgs(scaleDown);
        return scaleDown;
    }
}
