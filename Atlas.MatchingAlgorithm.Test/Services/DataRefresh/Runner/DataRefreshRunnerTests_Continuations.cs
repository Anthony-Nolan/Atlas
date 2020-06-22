using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Models.AzureManagement;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.DataRefresh;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh
{
    [TestFixture]
    public partial class DataRefreshRunnerTests
    {
        [TestCase(DataRefreshStage.MetadataDictionaryRefresh)]
        [TestCase(DataRefreshStage.DatabaseScalingSetup)]
        [TestCase(DataRefreshStage.DatabaseScalingTearDown)]
        // TODO: ATLAS-249: Add a test case for Queued Update Processing
        public async Task RefreshData_WhenUnskippableStageIsAlreadyComplete_DoesNotSkipStage(DataRefreshStage refreshStage)
        {
            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(DataRefreshRecordBuilder.New.WithStagesCompletedUpToAndIncluding(refreshStage).Build());

            await dataRefreshRunner.RefreshData(default);

            await dataRefreshHistoryRepository.Received(1).MarkStageAsComplete(Arg.Any<int>(), refreshStage);
        }

        [TestCase(DataRefreshStage.DataDeletion)]
        [TestCase(DataRefreshStage.DonorImport)]
        [TestCase(DataRefreshStage.DonorHlaProcessing)]
        public async Task RefreshData_WhenSkippableStageIsAlreadyComplete_SkipsStage(DataRefreshStage refreshStage)
        {
            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(DataRefreshRecordBuilder.New.WithStagesCompletedUpToAndIncluding(refreshStage).Build());

            await dataRefreshRunner.RefreshData(default);

            await dataRefreshHistoryRepository.DidNotReceive().MarkStageAsComplete(Arg.Any<int>(), refreshStage);
        }

        [Test]
        public async Task RefreshData_WhenRunWasPartiallyCompleteUpToDictionaryRefresh_RedoesDictionaryRefresh_AndContinuesFromDataDeletion()
        {
            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(
                DataRefreshRecordBuilder.New.WithStagesCompletedUpToAndIncluding(DataRefreshStage.MetadataDictionaryRefresh).Build()
            );

            await dataRefreshRunner.RefreshData(default);

            await hlaMetadataDictionary.ReceivedWithAnyArgs(1).RecreateHlaMetadataDictionary(default);
            await donorImportRepository.Received(1).RemoveAllDonorInformation();
        }

        [Test]
        public async Task RefreshData_WhenRunWasPartiallyCompleteUpToDataDeletion_ContinuesFromDatabaseScaling()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.RefreshDatabaseSize, AzureDatabaseSize.P15.ToString())
                .Build();
            settingsOptions.Value.Returns(settings);

            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(
                DataRefreshRecordBuilder.New.WithStagesCompletedUpToAndIncluding(DataRefreshStage.DataDeletion).Build()
            );

            await dataRefreshRunner.RefreshData(default);

            await donorImportRepository.DidNotReceive().RemoveAllDonorInformation();
            await azureDatabaseManager.Received(1).UpdateDatabaseSize(Arg.Any<string>(), AzureDatabaseSize.P15);
        }

        [Test]
        public async Task RefreshData_WhenRunWasPartiallyCompleteUpToDatabaseScaling_RedoesDatabaseScaling_AndContinuesFromDonorImport()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.RefreshDatabaseSize, AzureDatabaseSize.P15.ToString())
                .Build();
            settingsOptions.Value.Returns(settings);

            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(
                DataRefreshRecordBuilder.New.WithStagesCompletedUpToAndIncluding(DataRefreshStage.DatabaseScalingSetup).Build()
            );

            await dataRefreshRunner.RefreshData(default);

            await azureDatabaseManager.Received(1).UpdateDatabaseSize(default, AzureDatabaseSize.P15);
            await donorImporter.Received(1).ImportDonors();
        }

        [Test]
        public async Task RefreshData_WhenRunWasPartiallyCompleteUpToDonorImport_RepeatsNarrowDataDeletion_AndContinuesFromHlaProcessing()
        {
            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(
                DataRefreshRecordBuilder.New.WithStagesCompletedUpToAndIncluding(DataRefreshStage.DonorImport).Build()
            );

            await dataRefreshRunner.RefreshData(default);

            await donorImporter.DidNotReceive().ImportDonors();
            await donorImportRepository.DidNotReceive().RemoveAllDonorInformation();
            await donorImportRepository.Received(1).RemoveAllProcessedDonorHla();
            await hlaProcessor.ReceivedWithAnyArgs(1).UpdateDonorHla(default);
        }

        [Test]
        public async Task RefreshData_WhenRunWasPartiallyCompleteUpToHlaProcessing_ContinuesFromScalingTearDown()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.ActiveDatabaseSize, AzureDatabaseSize.S4.ToString())
                .Build();
            settingsOptions.Value.Returns(settings);

            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(
                DataRefreshRecordBuilder.New.WithStagesCompletedUpToAndIncluding(DataRefreshStage.DonorHlaProcessing).Build()
            );

            await dataRefreshRunner.RefreshData(default);

            await donorImportRepository.DidNotReceive().RemoveAllDonorInformation();
            await donorImportRepository.DidNotReceive().RemoveAllProcessedDonorHla();
            await hlaProcessor.DidNotReceiveWithAnyArgs().UpdateDonorHla(default);
            await azureDatabaseManager.Received(1).UpdateDatabaseSize(default, AzureDatabaseSize.S4);
        }

        [Test]
        public async Task RefreshData_WhenRunWasPartiallyCompleteUpToDatabaseScaling_RedoesDownScaling()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.ActiveDatabaseSize, AzureDatabaseSize.S4.ToString())
                .Build();
            settingsOptions.Value.Returns(settings);

            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(
                DataRefreshRecordBuilder.New.WithStagesCompletedUpToAndIncluding(DataRefreshStage.DatabaseScalingTearDown).Build()
            );

            await dataRefreshRunner.RefreshData(default);

            await azureDatabaseManager.Received(1).UpdateDatabaseSize(default, AzureDatabaseSize.S4);
        }

        [Test]
        public async Task RefreshData_WhenRunWasPartiallyCompleteUpToHlaIndexRecreation_DoesNotPerformInitialUpScalingOfDatabase()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.RefreshDatabaseSize, AzureDatabaseSize.P15.ToString())
                .Build();
            settingsOptions.Value.Returns(settings);

            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(
                DataRefreshRecordBuilder.New.WithStagesCompletedUpToAndIncluding(DataRefreshStage.IndexRecreation).Build()
            );

            await dataRefreshRunner.RefreshData(default);

            await azureDatabaseManager.DidNotReceive().UpdateDatabaseSize(default, AzureDatabaseSize.P15);
        }

        [Test]
        public async Task RefreshData_WhenContinuingFromDonorImport_ClearsAllData()
        {
            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(
                DataRefreshRecordBuilder.New.WithStagesCompletedUpToButNotIncluding(DataRefreshStage.DonorImport).Build()
            );

            await dataRefreshRunner.RefreshData(default);

            await donorImportRepository.Received(1).RemoveAllDonorInformation();
        }

        [Test]
        public async Task RefreshData_WhenContinuingFromSomeStepBeforeDonorImport_ButAfterInitialDataDeletion_DoesNotClearData()
        {
            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(
                DataRefreshRecordBuilder.New.WithStagesCompletedUpToButNotIncluding(DataRefreshStage.DatabaseScalingSetup).Build()
            );

            await dataRefreshRunner.RefreshData(default);

            await donorImportRepository.DidNotReceive().RemoveAllDonorInformation();
        }

        [Test]
        public async Task RefreshData_WhenContinuingFromHlaProcessingStep_DeletesOnlyProcessedHlaData()
        {
            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(
                DataRefreshRecordBuilder.New.WithStagesCompletedUpToButNotIncluding(DataRefreshStage.DonorHlaProcessing).Build()
            );

            await dataRefreshRunner.RefreshData(default);

            await donorImportRepository.DidNotReceive().RemoveAllDonorInformation();
            await donorImportRepository.Received().RemoveAllProcessedDonorHla();
        }

        [Test]
        public async Task RefreshData_WhenNotContinued_OnlyDeletesDataOnce()
        {
            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(
                DataRefreshRecordBuilder.New.Build()
            );

            await dataRefreshRunner.RefreshData(default);

            // Expected to be called exactly once in "DataDeletion" step, but not during donor import step
            await donorImportRepository.Received(1).RemoveAllDonorInformation();
            await donorImportRepository.DidNotReceive().RemoveAllProcessedDonorHla();
        }
    }
}