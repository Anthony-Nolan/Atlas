using System;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Services.DataRefresh;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.DataRefresh;
using AutoFixture;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh.Runner;

[TestFixture]
public partial class DataRefreshRunnerTests
{
    // ATL-491 experiment only: the JustPrecompute refresh.

    [Test]
    public async Task RefreshData_JustPrecompute_RemovesOnlyThePrecomputedData_AndRunsOnlyTheScaleUpAndThePrecomputation()
    {
        dataRefreshRunner = BuildDataRefreshRunner(DataRefreshSettingsBuilder.New
            .With(s => s.RefreshDatabaseSize, RefreshSize.ToString())
            .Build());
        var record = GivenAJustPrecomputeRecord();

        await dataRefreshRunner.RefreshData(record.Id);

        Received.InOrder(() =>
        {
            donorImportRepository.RemoveAllPrecomputedGenotypeSets();
            azureDatabaseManager.UpdateDatabaseSize(Arg.Any<string>(), RefreshSize, Arg.Any<int?>());
            donorGenotypePrecomputationStage.Run(record, Arg.Any<DataRefreshStageExecutionMode>(), Arg.Any<CancellationToken>());
        });
        await donorImportRepository.DidNotReceive().RemoveAllDonorInformation();
        await donorImportRepository.DidNotReceive().RemoveHlaTableIndexes();
        await donorImportRepository.DidNotReceive().CreateHlaTableIndexes();
        await donorImporter.DidNotReceiveWithAnyArgs().ImportDonors(default, default);
        await hlaProcessor.DidNotReceiveWithAnyArgs().UpdateDonorHla(default, default, default, default, default);
        await hlaMetadataDictionary.DidNotReceiveWithAnyArgs().RecreateHlaMetadataDictionary(default);
    }

    [Test]
    public async Task RefreshData_JustPrecompute_WhenTheDeletionIsCompleteAlready_DoesNotRemoveThePrecomputedDataAgain()
    {
        var record = GivenAJustPrecomputeRecord(DataRefreshStage.DataDeletion, DataRefreshStage.DatabaseScalingSetup);

        await dataRefreshRunner.RefreshData(record.Id);

        await donorImportRepository.DidNotReceive().RemoveAllPrecomputedGenotypeSets();
        await donorGenotypePrecomputationStage.Received(1).Run(record, Arg.Any<DataRefreshStageExecutionMode>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RefreshData_JustPrecompute_WithAPendingRetryDecision_RemovesThePrecomputedDataThenClearsTheDecisionThenRunsTheStage()
    {
        var record = GivenAJustPrecomputeRecord(DataRefreshStage.DataDeletion, DataRefreshStage.DatabaseScalingSetup);
        record.PendingManualDecision = DataRefreshManualDecision.RetryFailedBatches;

        await dataRefreshRunner.RefreshData(record.Id);

        Received.InOrder(() =>
        {
            donorImportRepository.RemoveAllPrecomputedGenotypeSets();
            dataRefreshHistoryRepository.ClearPendingManualDecision(record);
            donorGenotypePrecomputationStage.Run(record, Arg.Any<DataRefreshStageExecutionMode>(), Arg.Any<CancellationToken>());
        });
    }

    [Test]
    public async Task RefreshData_JustPrecompute_WithAPendingCompleteDecision_Throws_AndDoesNotComplete()
    {
        var record = GivenAJustPrecomputeRecord(DataRefreshStage.DataDeletion, DataRefreshStage.DatabaseScalingSetup);
        record.PendingManualDecision = DataRefreshManualDecision.Complete;

        var act = () => dataRefreshRunner.RefreshData(record.Id);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await dataRefreshHistoryRepository.DidNotReceiveWithAnyArgs().ApplyManualCompleteDecision(default);
    }

    private DataRefreshRecord GivenAJustPrecomputeRecord(params DataRefreshStage[] completedStages)
    {
        var record = DataRefreshRecordBuilder.New
            .With(r => r.Id, fixture.Create<int>())
            .With(r => r.JustPrecompute, true)
            .WithStagesCompleted(completedStages)
            .Build();
        dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(record);
        return record;
    }
}
