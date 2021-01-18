using System;
using System.Threading.Tasks;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Models.AzureManagement;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.DataRefresh;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh.Runner
{
    [TestFixture]
    public partial class DataRefreshRunnerTests
    {
        // These tests are extensions of the Test setup defined in DataRefreshRunnerTests_Core
        // Separated for convenience, since there are a LOT of tests :)

        [TestCase(DataRefreshStage.DatabaseScalingSetup)]
        [TestCase(DataRefreshStage.DatabaseScalingTearDown)]
        [TestCase(DataRefreshStage.QueuedDonorUpdateProcessing)]
        public async Task ContinuedRefreshData_WhenUnskippableStageIsAlreadyComplete_DoesNotSkipStage(DataRefreshStage refreshStage)
        {
            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(DataRefreshRecordBuilder.New.WithStagesCompletedUpToAndIncluding(refreshStage).Build());

            await dataRefreshRunner.RefreshData(default);

            await dataRefreshHistoryRepository.Received(1).MarkStageAsComplete(Arg.Any<DataRefreshRecord>(), refreshStage);
        }

        [TestCase(DataRefreshStage.MetadataDictionaryRefresh)]
        [TestCase(DataRefreshStage.IndexRemoval)]
        [TestCase(DataRefreshStage.DataDeletion)]
        [TestCase(DataRefreshStage.DonorImport)]
        [TestCase(DataRefreshStage.DonorHlaProcessing)]
        [TestCase(DataRefreshStage.IndexRecreation)]
        public async Task ContinuedRefreshData_WhenSkippableStageIsAlreadyComplete_SkipsStage(DataRefreshStage refreshStage)
        {
            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(DataRefreshRecordBuilder.New.WithStagesCompletedUpToAndIncluding(refreshStage).Build());

            await dataRefreshRunner.RefreshData(default);

            await dataRefreshHistoryRepository.DidNotReceive().MarkStageAsComplete(Arg.Any<DataRefreshRecord>(), refreshStage);
        }
        [Test]
        public async Task ContinuedRefreshData_WhenRunWasPartiallyCompleteUpToDictionaryRefresh_ContinuesFromIndexDeletion()
        {
            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(
                DataRefreshRecordBuilder.New.WithStagesCompletedUpToAndIncluding(DataRefreshStage.MetadataDictionaryRefresh).Build()
            );

            await dataRefreshRunner.RefreshData(default);

            await hlaMetadataDictionary.DidNotReceiveWithAnyArgs().RecreateHlaMetadataDictionary(default);
            await donorImportRepository.Received(1).RemoveHlaTableIndexes();
            await donorImportRepository.Received(1).RemoveAllDonorInformation();
        }

        [Test]
        public async Task RefreshData_WhenContinuingAfterMetadataDictionaryStep_WithNewLatestVersion_PassesStoredHlaVersionToLaterSteps()
        {
            var oldVersion = "olderHlaVersion";
            var newVersion = "latestHlaVersion";
            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(
                DataRefreshRecordBuilder.New
                    .WithStagesCompletedUpToAndIncluding(DataRefreshStage.MetadataDictionaryRefresh)
                    .With(r => r.HlaNomenclatureVersion, oldVersion)
                    .Build()
            );
            hlaMetadataDictionary.RecreateHlaMetadataDictionary(CreationBehaviour.Latest).Returns(newVersion);

            await dataRefreshRunner.RefreshData(default);

            await hlaProcessor.Received().UpdateDonorHla(oldVersion, Arg.Any<Func<int, Task>>());
        }

        [Test]
        public async Task ContinuedRefreshData_WhenRunWasPartiallyCompleteUpToIndexDeletion_ContinuesFromDataDeletion()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.RefreshDatabaseSize, AzureDatabaseSize.P15.ToString())
                .Build();
            dataRefreshRunner = BuildDataRefreshRunner(settings);

            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(
                DataRefreshRecordBuilder.New.WithStagesCompletedUpToAndIncluding(DataRefreshStage.IndexRemoval).Build()
            );

            await dataRefreshRunner.RefreshData(default);

            await donorImportRepository.DidNotReceive().RemoveHlaTableIndexes();
            await donorImportRepository.Received(1).RemoveAllDonorInformation();
            await azureDatabaseManager.Received(1).UpdateDatabaseSize(Arg.Any<string>(), AzureDatabaseSize.P15, Arg.Any<int?>());
        }

        [Test]
        public async Task ContinuedRefreshData_WhenRunWasPartiallyCompleteUpToDataDeletion_ContinuesFromDatabaseScaling()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.RefreshDatabaseSize, AzureDatabaseSize.P15.ToString())
                .Build();
            dataRefreshRunner = BuildDataRefreshRunner(settings);

            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(
                DataRefreshRecordBuilder.New.WithStagesCompletedUpToAndIncluding(DataRefreshStage.DataDeletion).Build()
            );

            await dataRefreshRunner.RefreshData(default);

            await donorImportRepository.DidNotReceive().RemoveAllDonorInformation();
            await donorImportRepository.DidNotReceive().RemoveHlaTableIndexes();
            await azureDatabaseManager.Received(1).UpdateDatabaseSize(Arg.Any<string>(), AzureDatabaseSize.P15, Arg.Any<int?>());
        }

        [Test]
        public async Task ContinuedRefreshData_WhenRunWasPartiallyCompleteUpToDatabaseScaling_RedoesDatabaseScaling_AndContinuesFromDonorImport()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.RefreshDatabaseSize, AzureDatabaseSize.P15.ToString())
                .Build();
            dataRefreshRunner = BuildDataRefreshRunner(settings);

            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(
                DataRefreshRecordBuilder.New.WithStagesCompletedUpToAndIncluding(DataRefreshStage.DatabaseScalingSetup).Build()
            );

            await dataRefreshRunner.RefreshData(default);

            await azureDatabaseManager.Received(1).UpdateDatabaseSize(default, AzureDatabaseSize.P15, Arg.Any<int?>());
            await donorImporter.Received(1).ImportDonors();
        }

        [Test]
        public async Task ContinuedRefreshData_WhenRunWasPartiallyCompleteUpToDonorImport_ContinuesFromHlaProcessing()
        {
            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(
                DataRefreshRecordBuilder.New.WithStagesCompletedUpToAndIncluding(DataRefreshStage.DonorImport).Build()
            );

            await dataRefreshRunner.RefreshData(default);

            await donorImporter.DidNotReceive().ImportDonors();
            await donorImportRepository.DidNotReceive().RemoveAllDonorInformation();
            await donorImportRepository.DidNotReceive().RemoveAllProcessedDonorHla();
            await hlaProcessor.Received().UpdateDonorHla(Arg.Any<string>(), Arg.Any<Func<int, Task>>(), Arg.Any<int?>(), true);
        }

        [Test]
        public async Task ContinuedRefreshData_WhenRunWasPartiallyCompleteUpToHlaProcessing_ContinuesFromIndexRecreation()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.ActiveDatabaseSize, AzureDatabaseSize.S4.ToString())
                .Build();
            dataRefreshRunner = BuildDataRefreshRunner(settings);

            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(
                DataRefreshRecordBuilder.New.WithStagesCompletedUpToAndIncluding(DataRefreshStage.DonorHlaProcessing).Build()
            );

            await dataRefreshRunner.RefreshData(default);

            await donorImportRepository.DidNotReceive().RemoveAllDonorInformation();
            await donorImportRepository.DidNotReceive().RemoveAllProcessedDonorHla();
            await hlaProcessor.DidNotReceiveWithAnyArgs().UpdateDonorHla(default, default);
            await donorImportRepository.Received(1).CreateHlaTableIndexes();
            await azureDatabaseManager.Received(1).UpdateDatabaseSize(default, AzureDatabaseSize.S4, Arg.Any<int?>());
        }

        [Test]
        public async Task ContinuedRefreshData_WhenRunWasPartiallyCompleteUpToIndexRecreation_ContinuesFromScalingTearDown()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.ActiveDatabaseSize, AzureDatabaseSize.S4.ToString())
                .Build();
            dataRefreshRunner = BuildDataRefreshRunner(settings);

            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(
                DataRefreshRecordBuilder.New.WithStagesCompletedUpToAndIncluding(DataRefreshStage.IndexRecreation).Build()
            );

            await dataRefreshRunner.RefreshData(default);

            await donorImportRepository.DidNotReceive().RemoveAllDonorInformation();
            await donorImportRepository.DidNotReceive().RemoveAllProcessedDonorHla();
            await hlaProcessor.DidNotReceiveWithAnyArgs().UpdateDonorHla(default, default);
            await donorImportRepository.DidNotReceive().CreateHlaTableIndexes();
            await azureDatabaseManager.Received(1).UpdateDatabaseSize(default, AzureDatabaseSize.S4, Arg.Any<int?>());
        }

        [Test]
        public async Task ContinuedRefreshData_WhenRunWasPartiallyCompleteUpToDatabaseScaling_RedoesDownScaling_AndContinuesToDonorUpdates()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.ActiveDatabaseSize, AzureDatabaseSize.S4.ToString())
                .Build();
            dataRefreshRunner = BuildDataRefreshRunner(settings);

            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(
                DataRefreshRecordBuilder.New.WithStagesCompletedUpToAndIncluding(DataRefreshStage.DatabaseScalingTearDown).Build()
            );

            await dataRefreshRunner.RefreshData(default);

            await azureDatabaseManager.Received(1).UpdateDatabaseSize(default, AzureDatabaseSize.S4, Arg.Any<int?>());
            await donorUpdateProcessor.ReceivedWithAnyArgs(1).ApplyDifferentialDonorUpdatesDuringRefresh(default, default);
        }

        [Test]
        public async Task ContinuedRefreshData_WhenRunWasPartiallyCompleteUpToQueuedDonorUpdates_RedoesScaleDown_AndRedoesDonorUpdates()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.ActiveDatabaseSize, AzureDatabaseSize.S4.ToString())
                .Build();
            dataRefreshRunner = BuildDataRefreshRunner(settings);

            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(
                DataRefreshRecordBuilder.New.WithStagesCompletedUpToAndIncluding(DataRefreshStage.QueuedDonorUpdateProcessing).Build()
            );

            await dataRefreshRunner.RefreshData(default);

            await azureDatabaseManager.Received(1).UpdateDatabaseSize(default, AzureDatabaseSize.S4, Arg.Any<int?>());
            await donorUpdateProcessor.ReceivedWithAnyArgs(1).ApplyDifferentialDonorUpdatesDuringRefresh(default, default);
        }

        [Test]
        public async Task ContinuedRefreshData_WhenRunWasPartiallyCompleteUpToHlaIndexRecreation_DoesNotPerformInitialUpScalingOfDatabase()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.RefreshDatabaseSize, AzureDatabaseSize.P15.ToString())
                .Build();
            dataRefreshRunner = BuildDataRefreshRunner(settings);

            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(
                DataRefreshRecordBuilder.New.WithStagesCompletedUpToAndIncluding(DataRefreshStage.IndexRecreation).Build()
            );

            await dataRefreshRunner.RefreshData(default);

            await azureDatabaseManager.DidNotReceive().UpdateDatabaseSize(default, AzureDatabaseSize.P15, Arg.Any<int?>());
        }

        [Test]
        public async Task ContinuedRefreshData_WhenRunWasPartiallyCompleteUpToDbScaleDown_DoesNotPerformInitialUpScalingOfDatabase()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.RefreshDatabaseSize, AzureDatabaseSize.P15.ToString())
                .Build();
            dataRefreshRunner = BuildDataRefreshRunner(settings);

            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(
                DataRefreshRecordBuilder.New.WithStagesCompletedUpToAndIncluding(DataRefreshStage.DatabaseScalingTearDown).Build()
            );

            await dataRefreshRunner.RefreshData(default);

            await azureDatabaseManager.DidNotReceive().UpdateDatabaseSize(default, AzureDatabaseSize.P15, Arg.Any<int?>());
        }

        [Test]
        public async Task ContinuedRefreshData_WhenContinuingFromDonorImport_ClearsAllData()
        {
            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(
                DataRefreshRecordBuilder.New.WithStagesCompletedUpToButNotIncluding(DataRefreshStage.DonorImport).Build()
            );

            await dataRefreshRunner.RefreshData(default);

            await donorImportRepository.Received(1).RemoveAllDonorInformation();
        }

        [Test]
        public async Task ContinuedRefreshData_WhenContinuingFromSomeStepBeforeDonorImport_ButAfterInitialDataDeletion_DoesNotClearData()
        {
            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(
                DataRefreshRecordBuilder.New.WithStagesCompletedUpToButNotIncluding(DataRefreshStage.DatabaseScalingSetup).Build()
            );

            await dataRefreshRunner.RefreshData(default);

            await donorImportRepository.DidNotReceive().RemoveAllDonorInformation();
        }

        [Test]
        public async Task ContinuedRefreshData_WhenContinuingFromHlaProcessingStep_DoesNotDeleteData()
        {
            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(
                DataRefreshRecordBuilder.New.WithStagesCompletedUpToButNotIncluding(DataRefreshStage.DonorHlaProcessing).Build()
            );

            await dataRefreshRunner.RefreshData(default);

            await donorImportRepository.DidNotReceive().RemoveAllDonorInformation();
            await donorImportRepository.DidNotReceive().RemoveAllProcessedDonorHla();
        }

        [Test]
        public async Task ContinuedRefreshData_WhenContinuingFromHlaProcessingStep_PassesContinuationArgCorrectly()
        {
            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(
                DataRefreshRecordBuilder.New.WithStagesCompletedUpToButNotIncluding(DataRefreshStage.DonorHlaProcessing).Build()
            );

            await dataRefreshRunner.RefreshData(default);

            await hlaProcessor.Received().UpdateDonorHla(Arg.Any<string>(), Arg.Any<Func<int,Task>>(), Arg.Any<int?>(), true);
        }

        [Test]
        public async Task ContinuedRefreshData_WhenNotContinued_OnlyDeletesDataOnce()
        {
            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(
                DataRefreshRecordBuilder.New.Build()
            );

            await dataRefreshRunner.RefreshData(default);

            // Expected to be called exactly once in "DataDeletion" step, but not during donor import step
            await donorImportRepository.Received(1).RemoveAllDonorInformation();
            await donorImportRepository.DidNotReceive().RemoveAllProcessedDonorHla();
        }

        [Test]
        public async Task ContinuedRefreshData_WhenAPreviousContinuationHasAlreadyBeenAttempted_ContinuesAppropriately()
        {
            const AzureDatabaseSize azureDatabaseSize = AzureDatabaseSize.S4;
            const int autoPauseDuration = 120;
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.ActiveDatabaseSize, azureDatabaseSize.ToString())
                .With(s => s.ActiveDatabaseAutoPauseTimeout, autoPauseDuration)
                .Build();
            dataRefreshRunner = BuildDataRefreshRunner(settings);

            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(
                DataRefreshRecordBuilder.New
                    .WithStagesCompletedUpToAndIncluding(DataRefreshStage.DonorHlaProcessing)
                    .WithSetup(r => { r.RefreshLastContinuedUtc = r.RefreshRequestedUtc.AddSeconds(1); })
                    .Build()
            );

            await dataRefreshRunner.RefreshData(default);

            await donorImportRepository.DidNotReceive().RemoveAllDonorInformation();
            await donorImportRepository.DidNotReceive().RemoveAllProcessedDonorHla();
            await hlaProcessor.DidNotReceiveWithAnyArgs().UpdateDonorHla(default, default);
            await donorImportRepository.Received(1).CreateHlaTableIndexes();
            await azureDatabaseManager.Received(1).UpdateDatabaseSize(default, azureDatabaseSize, autoPauseDuration);
        }

    }
}