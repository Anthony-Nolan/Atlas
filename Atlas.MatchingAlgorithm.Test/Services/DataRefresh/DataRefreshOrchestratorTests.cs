using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Models.AzureManagement;
using Atlas.MatchingAlgorithm.Services.AzureManagement;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Atlas.MatchingAlgorithm.Services.DataRefresh;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchingAlgorithm.Test.Builders.DataRefresh;
using Nova.Utils.ApplicationInsights;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh
{
    [TestFixture]
    public class DataRefreshOrchestratorTests
    {
        private ILogger logger;
        private IOptions<DataRefreshSettings> settingsOptions;
        private IWmdaHlaVersionProvider wmdaHlaVersionProvider;
        private IActiveDatabaseProvider activeDatabaseProvider;
        private IDataRefreshService dataRefreshService;
        private IDataRefreshHistoryRepository dataRefreshHistoryRepository;

        private IAzureFunctionManager azureFunctionManager;
        private IAzureDatabaseManager azureDatabaseManager;
        private IDataRefreshNotificationSender dataRefreshNotificationSender;

        private IDataRefreshOrchestrator dataRefreshOrchestrator;

        [SetUp]
        public void SetUp()
        {
            logger = Substitute.For<ILogger>();
            settingsOptions = Substitute.For<IOptions<DataRefreshSettings>>();
            wmdaHlaVersionProvider = Substitute.For<IWmdaHlaVersionProvider>();
            activeDatabaseProvider = Substitute.For<IActiveDatabaseProvider>();
            dataRefreshService = Substitute.For<IDataRefreshService>();
            dataRefreshHistoryRepository = Substitute.For<IDataRefreshHistoryRepository>();
            azureFunctionManager = Substitute.For<IAzureFunctionManager>();
            azureDatabaseManager = Substitute.For<IAzureDatabaseManager>();
            dataRefreshNotificationSender = Substitute.For<IDataRefreshNotificationSender>();

            settingsOptions.Value.Returns(DataRefreshSettingsBuilder.New.Build());
            wmdaHlaVersionProvider.GetActiveHlaDatabaseVersion().Returns("old");
            wmdaHlaVersionProvider.GetLatestStableHlaDatabaseVersion().Returns("new");

            dataRefreshOrchestrator = new DataRefreshOrchestrator(
                logger,
                settingsOptions,
                wmdaHlaVersionProvider,
                activeDatabaseProvider,
                dataRefreshService,
                dataRefreshHistoryRepository,
                azureFunctionManager,
                azureDatabaseManager,
                new AzureDatabaseNameProvider(settingsOptions),
                dataRefreshNotificationSender
            );
        }

        [Test]
        public async Task RefreshDataIfNecessary_WhenCurrentWmdaVersionMatchesLatest_DoesNotTriggerDataRefresh()
        {
            const string wmdaVersion = "3330";
            wmdaHlaVersionProvider.GetActiveHlaDatabaseVersion().Returns(wmdaVersion);
            wmdaHlaVersionProvider.GetLatestStableHlaDatabaseVersion().Returns(wmdaVersion);

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshService.DidNotReceive().RefreshData(Arg.Any<string>());
        }
        
        [Test]
        public async Task RefreshDataIfNecessary_WhenCurrentWmdaVersionMatchesLatest_AndShouldForceRefresh_TriggersDataRefresh()
        {
            const string wmdaVersion = "3330";
            wmdaHlaVersionProvider.GetActiveHlaDatabaseVersion().Returns(wmdaVersion);
            wmdaHlaVersionProvider.GetLatestStableHlaDatabaseVersion().Returns(wmdaVersion);

            await dataRefreshOrchestrator.RefreshDataIfNecessary(shouldForceRefresh: true);

            await dataRefreshService.Received().RefreshData(Arg.Any<string>());
        }

        [Test]
        public async Task RefreshDataIfNecessary_WhenLatestWmdaVersionHigherThanCurrent_TriggersDataRefresh()
        {
            wmdaHlaVersionProvider.GetActiveHlaDatabaseVersion().Returns("3330");
            wmdaHlaVersionProvider.GetLatestStableHlaDatabaseVersion().Returns("3370");

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshService.Received().RefreshData(Arg.Any<string>());
        }

        [Test]
        public async Task RefreshDataIfNecessary_WhenLatestWmdaVersionHigherThanCurrent_AndJobAlreadyInProgress_DoesNotTriggerDataRefresh()
        {
            wmdaHlaVersionProvider.GetActiveHlaDatabaseVersion().Returns("3330");
            wmdaHlaVersionProvider.GetLatestStableHlaDatabaseVersion().Returns("3370");
            dataRefreshHistoryRepository.GetInProgressJobs().Returns(new List<DataRefreshRecord> {new DataRefreshRecord()});

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshService.DidNotReceive().RefreshData(Arg.Any<string>());
        }
        
        [Test]
        public async Task RefreshDataIfNecessary_WhenShouldForceRefresh_AndJobAlreadyInProgress_DoesNotTriggerDataRefresh()
        {
            dataRefreshHistoryRepository.GetInProgressJobs().Returns(new List<DataRefreshRecord> {new DataRefreshRecord()});

            await dataRefreshOrchestrator.RefreshDataIfNecessary(shouldForceRefresh: true);

            await dataRefreshService.DidNotReceive().RefreshData(Arg.Any<string>());
        }

        [Test]
        public async Task RefreshDataIfNecessary_TriggersDataRefreshWithLatestWmdaVersion()
        {
            const string latestWmdaVersion = "3370";
            wmdaHlaVersionProvider.GetLatestStableHlaDatabaseVersion().Returns(latestWmdaVersion);

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshService.Received().RefreshData(latestWmdaVersion);
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
        public async Task RefreshDataIfNecessary_StoresRefreshRecordOfWmdaVersion()
        {
            const string latestWmdaVersion = "latest-wmda";
            wmdaHlaVersionProvider.GetLatestStableHlaDatabaseVersion().Returns(latestWmdaVersion);

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshHistoryRepository.Received().Create(Arg.Is<DataRefreshRecord>(r =>
                r.WmdaDatabaseVersion == latestWmdaVersion
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

            await dataRefreshHistoryRepository.Received().UpdateFinishTime(Arg.Any<int>(), Arg.Any<DateTime>());
        }

        [Test]
        public async Task RefreshDataIfNecessary_WhenDataRefreshFails_LogsExceptionDetails()
        {
            const string exceptionMessage = "something very bad happened";
            dataRefreshService.RefreshData(Arg.Any<string>()).Throws(new Exception(exceptionMessage));

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            logger.Received().SendTrace(Arg.Is<string>(e => e.Contains(exceptionMessage)), LogLevel.Critical);
        }

        [Test]
        public async Task RefreshDataIfNecessary_WhenDataRefreshFails_StoresFinishTime()
        {
            const string exceptionMessage = "something very bad happened";
            dataRefreshService.RefreshData(Arg.Any<string>()).Throws(new Exception(exceptionMessage));

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshHistoryRepository.Received().UpdateFinishTime(Arg.Any<int>(), Arg.Any<DateTime>());
        }

        [Test]
        public async Task RefreshDataIfNecessary_WhenDataRefreshFails_StoresSuccessFlagAsFalse()
        {
            const string exceptionMessage = "something very bad happened";
            dataRefreshService.RefreshData(Arg.Any<string>()).Throws(new Exception(exceptionMessage));

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
            dataRefreshService.RefreshData(Arg.Any<string>()).Throws(new Exception());
            
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
            dataRefreshService.RefreshData(Arg.Any<string>()).Throws(new Exception());

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
            dataRefreshService.RefreshData(Arg.Any<string>()).Throws(new Exception());
            
            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshNotificationSender.Received().SendFailureAlert();
        }
    }
}