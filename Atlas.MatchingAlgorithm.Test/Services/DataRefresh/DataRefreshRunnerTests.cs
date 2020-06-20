using System;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Models.AzureManagement;
using Atlas.MatchingAlgorithm.Services.AzureManagement;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DataRefresh;
using Atlas.MatchingAlgorithm.Services.DataRefresh.DonorImport;
using Atlas.MatchingAlgorithm.Services.DataRefresh.HlaProcessing;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.DataRefresh;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh
{
    [TestFixture]
    public class DataRefreshRunnerTests
    {
        private IOptions<DataRefreshSettings> settingsOptions;
        private IActiveDatabaseProvider activeDatabaseProvider;
        private IAzureDatabaseManager azureDatabaseManager;
        private IDonorImportRepository donorImportRepository;
        private IHlaMetadataDictionary hlaMetadataDictionary;
        private IDonorImporter donorImporter;
        private IHlaProcessor hlaProcessor;
        private IDataRefreshNotificationSender dataRefreshNotificationSender;
        private IDataRefreshHistoryRepository dataRefreshHistoryRepository;

        private IDataRefreshRunner dataRefreshRunner;
        private ILogger logger;
        private IDormantRepositoryFactory transientRepositoryFactory;

        [SetUp]
        public void SetUp()
        {
            settingsOptions = Substitute.For<IOptions<DataRefreshSettings>>();
            activeDatabaseProvider = Substitute.For<IActiveDatabaseProvider>();
            azureDatabaseManager = Substitute.For<IAzureDatabaseManager>();
            donorImportRepository = Substitute.For<IDonorImportRepository>();
            transientRepositoryFactory = Substitute.For<IDormantRepositoryFactory>();
            hlaMetadataDictionary = Substitute.For<IHlaMetadataDictionary>();
            donorImporter = Substitute.For<IDonorImporter>();
            hlaProcessor = Substitute.For<IHlaProcessor>();
            logger = Substitute.For<ILogger>();
            dataRefreshNotificationSender = Substitute.For<IDataRefreshNotificationSender>();
            dataRefreshHistoryRepository = Substitute.For<IDataRefreshHistoryRepository>();

            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(DataRefreshRecordBuilder.New.Build());
            transientRepositoryFactory.GetDonorImportRepository().Returns(donorImportRepository);
            settingsOptions.Value.Returns(DataRefreshSettingsBuilder.New.Build());

            dataRefreshRunner = new DataRefreshRunner(
                settingsOptions,
                activeDatabaseProvider,
                new AzureDatabaseNameProvider(settingsOptions),
                azureDatabaseManager,
                transientRepositoryFactory,
                new HlaMetadataDictionaryBuilder().Returning(hlaMetadataDictionary),
                Substitute.For<IActiveHlaNomenclatureVersionAccessor>(),
                donorImporter,
                hlaProcessor,
                logger,
                dataRefreshNotificationSender,
                dataRefreshHistoryRepository
            );
        }

        [Test]
        public async Task RefreshData_ScalesDormantDatabase()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DatabaseAName, "db-a")
                .With(s => s.DatabaseBName, "db-b")
                .Build();
            settingsOptions.Value.Returns(settings);
            activeDatabaseProvider.GetDormantDatabase().Returns(TransientDatabase.DatabaseA);

            await dataRefreshRunner.RefreshData(default);

            await azureDatabaseManager.UpdateDatabaseSize(settings.DatabaseAName, Arg.Any<AzureDatabaseSize>());
        }

        [Test]
        public async Task RefreshData_ScalesDormantDatabaseToRefreshSize()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.RefreshDatabaseSize, "P15")
                .Build();
            settingsOptions.Value.Returns(settings);

            await dataRefreshRunner.RefreshData(default);

            await azureDatabaseManager.UpdateDatabaseSize(Arg.Any<string>(), AzureDatabaseSize.P15);
        }

        [Test]
        public async Task RefreshData_RemovesAllDonorInformation()
        {
            await dataRefreshRunner.RefreshData(default);

            await donorImportRepository.Received().RemoveAllDonorInformation();
        }

        [Test]
        public async Task RefreshData_ReportsHlaMetadataWasRecreated_WithRefreshHlaNomenclatureVersion()
        {
            const string hlaNomenclatureVersion = "3390";
            hlaMetadataDictionary.IsActiveVersionDifferentFromLatestVersion().Returns(true);
            hlaMetadataDictionary
                .RecreateHlaMetadataDictionary(CreationBehaviour.Latest)
                .Returns(hlaNomenclatureVersion);

            var returnedHlaVersion = await dataRefreshRunner.RefreshData(default);

            returnedHlaVersion.Should().Be(hlaNomenclatureVersion);
        }

        [Test]
        public async Task RefreshData_ImportsDonors()
        {
            await dataRefreshRunner.RefreshData(default);

            await donorImporter.Received().ImportDonors();
        }

        [Test]
        public async Task RefreshData_ProcessesDonorHla_WithRefreshHlaNomenclatureVersion()
        {
            const string hlaNomenclatureVersion = "3390";
            hlaMetadataDictionary.IsActiveVersionDifferentFromLatestVersion().Returns(true);
            hlaMetadataDictionary
                .RecreateHlaMetadataDictionary(CreationBehaviour.Latest)
                .Returns(hlaNomenclatureVersion);

            await dataRefreshRunner.RefreshData(default);

            await hlaProcessor.Received().UpdateDonorHla(hlaNomenclatureVersion);
        }

        [Test]
        public async Task RefreshData_ScalesRefreshDatabaseToActiveSize()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.RefreshDatabaseSize, "S4")
                .Build();
            settingsOptions.Value.Returns(settings);

            await dataRefreshRunner.RefreshData(default);

            await azureDatabaseManager.UpdateDatabaseSize(Arg.Any<string>(), AzureDatabaseSize.S4);
        }

        [Test]
        public async Task RefreshData_RunsAzureSetUp_BeforeRefresh()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.RefreshDatabaseSize, "P15")
                .Build();
            settingsOptions.Value.Returns(settings);

            await dataRefreshRunner.RefreshData(default);

            Received.InOrder(() =>
            {
                azureDatabaseManager.UpdateDatabaseSize(Arg.Any<string>(), AzureDatabaseSize.P15);
                donorImporter.ImportDonors();
            });
        }

        [Test]
        public async Task RefreshData_RunsAzureTearDown_AfterRefresh()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.ActiveDatabaseSize, "S4")
                .Build();
            settingsOptions.Value.Returns(settings);

            await dataRefreshRunner.RefreshData(default);

            Received.InOrder(() =>
            {
                donorImporter.ImportDonors();
                azureDatabaseManager.UpdateDatabaseSize(Arg.Any<string>(), AzureDatabaseSize.S4);
            });
        }

        [Test]
        public async Task RefreshData_RunsAzureDatabaseSetUp_BeforeTearDown()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.ActiveDatabaseSize, "S4")
                .With(s => s.RefreshDatabaseSize, "P15")
                .Build();
            settingsOptions.Value.Returns(settings);

            await dataRefreshRunner.RefreshData(default);

            Received.InOrder(() =>
            {
                azureDatabaseManager.UpdateDatabaseSize(Arg.Any<string>(), AzureDatabaseSize.P15);
                azureDatabaseManager.UpdateDatabaseSize(Arg.Any<string>(), AzureDatabaseSize.S4);
            });
        }

        [Test]
        public async Task RefreshData_WhenHlaMetadataDictionaryRecreationFails_ScalesRefreshDatabaseToDormantSize()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DatabaseAName, "db-a")
                .With(s => s.DormantDatabaseSize, "S0")
                .Build();
            settingsOptions.Value.Returns(settings);
            activeDatabaseProvider.GetDormantDatabase().Returns(TransientDatabase.DatabaseA);
            hlaMetadataDictionary.RecreateHlaMetadataDictionary(CreationBehaviour.Latest).Throws(new Exception());

            try
            {
                await dataRefreshRunner.RefreshData(default);
            }
            catch (Exception)
            {
                await azureDatabaseManager.Received().UpdateDatabaseSize(settings.DatabaseAName, AzureDatabaseSize.S0);
            }
        }

        [Test]
        public async Task RefreshData_WhenDonorImportFails_ScalesRefreshDatabaseToDormantSize()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DatabaseAName, "db-a")
                .With(s => s.DormantDatabaseSize, "S0")
                .Build();
            settingsOptions.Value.Returns(settings);
            activeDatabaseProvider.GetDormantDatabase().Returns(TransientDatabase.DatabaseA);
            donorImporter.ImportDonors().Throws(new Exception());

            try
            {
                await dataRefreshRunner.RefreshData(default);
            }
            catch (Exception)
            {
                await azureDatabaseManager.Received().UpdateDatabaseSize(settings.DatabaseAName, AzureDatabaseSize.S0);
            }
        }

        [Test]
        public async Task RefreshData_WhenHlaProcessingFails_ScalesRefreshDatabaseToDormantSize()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DatabaseAName, "db-a")
                .With(s => s.DormantDatabaseSize, "S0")
                .Build();
            settingsOptions.Value.Returns(settings);
            activeDatabaseProvider.GetDormantDatabase().Returns(TransientDatabase.DatabaseA);
            hlaProcessor.UpdateDonorHla(Arg.Any<string>()).Throws(new Exception());

            try
            {
                await dataRefreshRunner.RefreshData(default);
            }
            catch (Exception)
            {
                await azureDatabaseManager.Received().UpdateDatabaseSize(settings.DatabaseAName, AzureDatabaseSize.S0);
            }
        }

        [Test]
        public async Task RefreshData_WhenTeardownFails_SendsAlert()
        {
            const AzureDatabaseSize databaseSize = AzureDatabaseSize.S0;
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DatabaseAName, "db-a")
                .With(s => s.DormantDatabaseSize, databaseSize.ToString())
                .Build();
            settingsOptions.Value.Returns(settings);
            activeDatabaseProvider.GetDormantDatabase().Returns(TransientDatabase.DatabaseA);
            hlaProcessor.UpdateDonorHla(Arg.Any<string>()).Throws(new Exception());
            azureDatabaseManager.UpdateDatabaseSize(Arg.Any<string>(), databaseSize).Throws(new Exception());

            try
            {
                await dataRefreshRunner.RefreshData(default);
            }
            catch (Exception)
            {
                await dataRefreshNotificationSender.ReceivedWithAnyArgs().SendTeardownFailureAlert(default);
            }
        }

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