using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders;
using Atlas.HlaMetadataDictionary.WmdaDataAccess;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Models.AzureManagement;
using Atlas.MatchingAlgorithm.Services.AzureManagement;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Atlas.MatchingAlgorithm.Services.DataRefresh;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.DataRefresh;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh
{
    [TestFixture]
    public class DataRefreshOrchestratorTests
    {
        private ILogger logger;
        private IOptions<DataRefreshSettings> settingsOptions;
        private IWmdaHlaNomenclatureVersionAccessor wmdaHlaNomenclatureVersionAccessor;
        private IActiveDatabaseProvider activeDatabaseProvider;
        private IDataRefreshRunner dataRefreshRunner;
        private IDataRefreshHistoryRepository dataRefreshHistoryRepository;

        private IAzureFunctionManager azureFunctionManager;
        private IAzureDatabaseManager azureDatabaseManager;
        private IDataRefreshNotificationSender dataRefreshNotificationSender;

        private IDataRefreshOrchestrator dataRefreshOrchestrator;
        private string existingHlaVersion = "old";
        private string newHlaVersion = "new";

        [SetUp]
        public void SetUp()
        {

            settingsOptions = Substitute.For<IOptions<DataRefreshSettings>>();
            settingsOptions.Value.Returns(DataRefreshSettingsBuilder.New.Build());

            wmdaHlaNomenclatureVersionAccessor = Substitute.For<IWmdaHlaNomenclatureVersionAccessor>();
            var activeHlaVersionAccessor = Substitute.For<IActiveHlaNomenclatureVersionAccessor>();
            activeHlaVersionAccessor.GetActiveHlaNomenclatureVersion().Returns(existingHlaVersion);

            var hlaMetadataDictionaryBuilder = new HlaMetadataDictionaryBuilder().Using(wmdaHlaNomenclatureVersionAccessor);

            logger = Substitute.For<ILogger>();
            activeDatabaseProvider = Substitute.For<IActiveDatabaseProvider>();
            dataRefreshRunner = Substitute.For<IDataRefreshRunner>();
            dataRefreshHistoryRepository = Substitute.For<IDataRefreshHistoryRepository>();
            azureFunctionManager = Substitute.For<IAzureFunctionManager>();
            azureDatabaseManager = Substitute.For<IAzureDatabaseManager>();
            dataRefreshNotificationSender = Substitute.For<IDataRefreshNotificationSender>();

            dataRefreshOrchestrator = new DataRefreshOrchestrator(
                logger,
                settingsOptions,
                hlaMetadataDictionaryBuilder,
                activeHlaVersionAccessor,
                activeDatabaseProvider,
                dataRefreshRunner,
                dataRefreshHistoryRepository,
                azureFunctionManager,
                azureDatabaseManager,
                new AzureDatabaseNameProvider(settingsOptions),
                dataRefreshNotificationSender
            );
        }

        [Test]
        public async Task RefreshDataIfNecessary_WhenActiveHlaVersionMatchesLatest_DoesNotTriggerDataRefresh()
        {
            wmdaHlaNomenclatureVersionAccessor.GetLatestStableHlaNomenclatureVersion().ReturnsForAnyArgs(existingHlaVersion);

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshRunner.DidNotReceiveWithAnyArgs().RefreshData(default);
        }
        
        [Test]
        public async Task RefreshDataIfNecessary_WhenActiveHlaVersionMatchesLatest_AndShouldForceRefresh_TriggersDataRefresh()
        {
            wmdaHlaNomenclatureVersionAccessor.GetLatestStableHlaNomenclatureVersion().ReturnsForAnyArgs(existingHlaVersion);

            await dataRefreshOrchestrator.RefreshDataIfNecessary(shouldForceRefresh: true);

            await dataRefreshRunner.ReceivedWithAnyArgs().RefreshData(default);
        }

        [Test]
        public async Task RefreshDataIfNecessary_WhenLatestHlaVersionHigherThanCurrent_TriggersDataRefresh()
        {
            wmdaHlaNomenclatureVersionAccessor.GetLatestStableHlaNomenclatureVersion().ReturnsForAnyArgs(newHlaVersion);

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshRunner.ReceivedWithAnyArgs().RefreshData(default);
        }

        [Test]
        public async Task RefreshDataIfNecessary_WhenLatestHlaVersionHigherThanCurrent_AndJobAlreadyInProgress_DoesNotTriggerDataRefresh()
        {
            wmdaHlaNomenclatureVersionAccessor.GetLatestStableHlaNomenclatureVersion().ReturnsForAnyArgs(newHlaVersion);
            dataRefreshHistoryRepository.GetInProgressJobs().Returns(new List<DataRefreshRecord> {new DataRefreshRecord()});

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshRunner.DidNotReceiveWithAnyArgs().RefreshData(default);
        }
        
        [Test]
        public async Task RefreshDataIfNecessary_WhenShouldForceRefresh_AndJobAlreadyInProgress_DoesNotTriggerDataRefresh()
        {
            dataRefreshHistoryRepository.GetInProgressJobs().Returns(new List<DataRefreshRecord> {new DataRefreshRecord()});

            await dataRefreshOrchestrator.RefreshDataIfNecessary(shouldForceRefresh: true);

            await dataRefreshRunner.DidNotReceiveWithAnyArgs().RefreshData(default);
        }

        [Test]
        public async Task RefreshDataIfNecessary_RecordsInitialDataRefreshWithNoWmdaVersion()
        {
            wmdaHlaNomenclatureVersionAccessor.GetLatestStableHlaNomenclatureVersion().ReturnsForAnyArgs(newHlaVersion);

            await dataRefreshOrchestrator.RefreshDataIfNecessary();
            await dataRefreshHistoryRepository.Received().Create(Arg.Is<DataRefreshRecord>(r => string.IsNullOrWhiteSpace(r.HlaNomenclatureVersion)));
        }

        [Test]
        public async Task RefreshDataIfNecessary_EventuallyRecordsDataRefreshOccurredWithLatestWmdaVersion()
        {
            wmdaHlaNomenclatureVersionAccessor.GetLatestStableHlaNomenclatureVersion().ReturnsForAnyArgs(newHlaVersion);
            dataRefreshRunner.RefreshData(Arg.Any<int>()).Returns(newHlaVersion);

            await dataRefreshOrchestrator.RefreshDataIfNecessary();
            await dataRefreshHistoryRepository.Received().UpdateExecutionDetails(Arg.Any<int>(), newHlaVersion, Arg.Any<DateTime?>());
        }

        [Test]
        public async Task RefreshDataIfNecessary_StoresDataRefreshRecordWithNoEndTime()
        {
            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshHistoryRepository.Received().Create(Arg.Is<DataRefreshRecord>(r =>
                r.RefreshEndUtc == null
            ));
        }

        [Test]
        public async Task RefreshDataIfNecessary_StoresDataRefreshRecordWithNoSuccessStatus()
        {
            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshHistoryRepository.Received().Create(Arg.Is<DataRefreshRecord>(r =>
                r.WasSuccessful == null
            ));
        }

        [Test]
        public async Task RefreshDataIfNecessary_WhenDatabaseAActive_StoresRefreshRecordOfDatabaseB()
        {
            activeDatabaseProvider.GetDormantDatabase().Returns(TransientDatabase.DatabaseB);

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshHistoryRepository.Received().Create(Arg.Is<DataRefreshRecord>(r =>
                r.Database == "DatabaseB"
            ));
        }

        [Test]
        public async Task RefreshDataIfNecessary_WhenDatabaseBActive_StoresRefreshRecordOfDatabaseA()
        {
            activeDatabaseProvider.GetDormantDatabase().Returns(TransientDatabase.DatabaseA);

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshHistoryRepository.Received().Create(Arg.Is<DataRefreshRecord>(r =>
                r.Database == "DatabaseA"
            ));
        }

        [Test]
        public async Task RefreshDataIfNecessary_WhenJobSuccessful_StoresRecordAsSuccess()
        {
            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshHistoryRepository.Received().UpdateSuccessFlag(Arg.Any<int>(), true);
        }

        [Test]
        public async Task RefreshDataIfNecessary_WhenJobSuccessful_StoresFinishTime()
        {
            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshHistoryRepository.ReceivedWithAnyArgs().UpdateExecutionDetails(default, default, default);
        }

        [Test]
        public async Task RefreshDataIfNecessary_WhenDataRefreshFails_LogsExceptionDetails()
        {
            const string exceptionMessage = "something very bad happened";
            dataRefreshRunner.RefreshData(Arg.Any<int>()).Throws(new Exception(exceptionMessage));

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            logger.Received().SendTrace(Arg.Is<string>(e => e.Contains(exceptionMessage)), LogLevel.Critical);
        }

        [Test]
        public async Task RefreshDataIfNecessary_WhenDataRefreshFails_StoresFinishTime()
        {
            const string exceptionMessage = "something very bad happened";
            dataRefreshRunner.RefreshData(Arg.Any<int>()).Throws(new Exception(exceptionMessage));

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshHistoryRepository.ReceivedWithAnyArgs().UpdateExecutionDetails(default, default, default);
        }

        [Test]
        public async Task RefreshDataIfNecessary_WhenDataRefreshFails_StoresSuccessFlagAsFalse()
        {
            const string exceptionMessage = "something very bad happened";
            dataRefreshRunner.RefreshData(Arg.Any<int>()).Throws(new Exception(exceptionMessage));

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshHistoryRepository.Received().UpdateSuccessFlag(Arg.Any<int>(), false);
        }
        
        [Test]
        public async Task RefreshData_StopsDonorImportFunction()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DonorFunctionsAppName, "functions-app")
                .With(s => s.DonorImportFunctionName, "import-func")
                .Build();
            settingsOptions.Value.Returns(settings);

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await azureFunctionManager.Received().StopFunction(settings.DonorFunctionsAppName, settings.DonorImportFunctionName);
        }

        [Test]
        public async Task RefreshData_RestartsDonorImportFunction()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DonorFunctionsAppName, "functions-app")
                .With(s => s.DonorImportFunctionName, "import-func")
                .Build();
            settingsOptions.Value.Returns(settings);

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await azureFunctionManager.Received().StartFunction(settings.DonorFunctionsAppName, settings.DonorImportFunctionName);
        }
        
        [Test]
        public async Task RefreshData_ScalesActiveDatabaseToDormantSize()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DatabaseAName, "db-a")
                .With(s => s.DormantDatabaseSize, "S0")
                .Build();
            settingsOptions.Value.Returns(settings);
            activeDatabaseProvider.GetActiveDatabase().Returns(TransientDatabase.DatabaseA);

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await azureDatabaseManager.Received().UpdateDatabaseSize(settings.DatabaseAName, AzureDatabaseSize.S0);
        }
        
        [Test]
        public async Task RefreshData_ScalesDownDatabaseThatWasActiveWhenTheJobStarted()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DatabaseAName, "db-a")
                .With(s => s.DormantDatabaseSize, "S0")
                .Build();
            settingsOptions.Value.Returns(settings);
            activeDatabaseProvider.GetActiveDatabase().Returns(TransientDatabase.DatabaseA);
            
            // Marking refresh record as complete will switch over which database is considered "active". Emulating this with mocks here.
            dataRefreshHistoryRepository.WhenForAnyArgs(r => r.UpdateSuccessFlag(0, true)).Do(x =>
            {
                activeDatabaseProvider.GetActiveDatabase().Returns(TransientDatabase.DatabaseB);
            });

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await azureDatabaseManager.Received().UpdateDatabaseSize(settings.DatabaseAName, AzureDatabaseSize.S0);
        }
        
        [Test]
        public async Task RefreshData_WhenRefreshFails_DoesNotScaleActiveDatabaseToDormantSize()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DatabaseAName, "db-a")
                .With(s => s.DormantDatabaseSize, "S0")
                .Build();
            settingsOptions.Value.Returns(settings);
            activeDatabaseProvider.GetActiveDatabase().Returns(TransientDatabase.DatabaseA);
            dataRefreshRunner.RefreshData(Arg.Any<int>()).Throws(new Exception());
            
            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await azureDatabaseManager.DidNotReceive().UpdateDatabaseSize(settings.DatabaseAName, AzureDatabaseSize.S0);
        }
        
        [Test]
        public async Task RefreshData_WhenRefreshFails_RestartsDonorImportFunction()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DonorFunctionsAppName, "functions-app")
                .With(s => s.DonorImportFunctionName, "import-func")
                .Build();
            settingsOptions.Value.Returns(settings);
            dataRefreshRunner.RefreshData(Arg.Any<int>()).Throws(new Exception());

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await azureFunctionManager.Received().StartFunction(settings.DonorFunctionsAppName, settings.DonorImportFunctionName);
        }
        
        [Test]
        public async Task RefreshData_SendsNotificationOnInitialisation()
        {
            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshNotificationSender.Received().SendInitialisationNotification();
        }
        
        [Test]
        public async Task RefreshData_SendsNotificationOnSuccess()
        {
            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshNotificationSender.Received().SendSuccessNotification();
        }
        
        [Test]
        public async Task RefreshData_SendsAlertOnFailure()
        {
            dataRefreshRunner.RefreshData(Arg.Any<int>()).Throws(new Exception());
            
            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshNotificationSender.Received().SendFailureAlert();
        }
    }
}