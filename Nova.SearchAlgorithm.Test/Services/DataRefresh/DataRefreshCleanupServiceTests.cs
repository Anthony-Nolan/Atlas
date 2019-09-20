using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Nova.SearchAlgorithm.Data.Persistent.Models;
using Nova.SearchAlgorithm.Data.Persistent.Repositories;
using Nova.SearchAlgorithm.Models.AzureManagement;
using Nova.SearchAlgorithm.Services.AzureManagement;
using Nova.SearchAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Nova.SearchAlgorithm.Services.DataRefresh;
using Nova.SearchAlgorithm.Settings;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.Notifications;
using NSubstitute;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Services.DataRefresh
{
    [TestFixture]
    public class DataRefreshCleanupServiceTests
    {
        private ILogger logger;
        private IAzureDatabaseNameProvider azureDatabaseNameProvider;
        private IActiveDatabaseProvider activeDatabaseProvider;
        private IAzureDatabaseManager azureDatabaseManager;
        private IOptions<DataRefreshSettings> dataRefreshOptions;
        private IAzureFunctionManager azureFunctionManager;
        private IDataRefreshHistoryRepository dataRefreshHistoryRepository;
        private INotificationsClient notificationsClient;

        private IDataRefreshCleanupService dataRefreshCleanupService;

        [SetUp]
        public void SetUp()
        {
            logger = Substitute.For<ILogger>();
            azureDatabaseNameProvider = Substitute.For<IAzureDatabaseNameProvider>();
            activeDatabaseProvider = Substitute.For<IActiveDatabaseProvider>();
            azureDatabaseManager = Substitute.For<IAzureDatabaseManager>();
            dataRefreshOptions = Substitute.For<IOptions<DataRefreshSettings>>();
            azureFunctionManager = Substitute.For<IAzureFunctionManager>();
            dataRefreshHistoryRepository = Substitute.For<IDataRefreshHistoryRepository>();
            notificationsClient = Substitute.For<INotificationsClient>();

            dataRefreshOptions.Value.Returns(new DataRefreshSettings {DormantDatabaseSize = "S0"});

            dataRefreshCleanupService = new DataRefreshCleanupService(
                logger,
                azureDatabaseNameProvider,
                activeDatabaseProvider,
                azureDatabaseManager,
                dataRefreshOptions,
                azureFunctionManager,
                dataRefreshHistoryRepository,
                notificationsClient);
        }

        [Test]
        public async Task RunDataRefreshCleanup_WhenNoJobsInProgress_DoesNotScaleDatabase()
        {
            dataRefreshHistoryRepository.GetInProgressJobs().Returns(new List<DataRefreshRecord>());

            await dataRefreshCleanupService.RunDataRefreshCleanup();

            await azureDatabaseManager.DidNotReceive().UpdateDatabaseSize(Arg.Any<string>(), Arg.Any<AzureDatabaseSize>());
        }

        [Test]
        public async Task RunDataRefreshCleanup_WhenNoJobsInProgress_DoesNotEnableFunctions()
        {
            dataRefreshHistoryRepository.GetInProgressJobs().Returns(new List<DataRefreshRecord>());

            await dataRefreshCleanupService.RunDataRefreshCleanup();

            await azureFunctionManager.DidNotReceive().StartFunction(Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        public async Task RunDataRefreshCleanup_ScalesDormantDatabaseToDormantSize()
        {
            const string databaseName = "db-name";
            dataRefreshHistoryRepository.GetInProgressJobs().Returns(new List<DataRefreshRecord> {new DataRefreshRecord()});
            azureDatabaseNameProvider.GetDatabaseName(Arg.Any<TransientDatabase>()).Returns(databaseName);
            dataRefreshOptions.Value.Returns(new DataRefreshSettings {DormantDatabaseSize = "S1"});

            await dataRefreshCleanupService.RunDataRefreshCleanup();

            await azureDatabaseManager.Received().UpdateDatabaseSize(databaseName, AzureDatabaseSize.S1);
        }

        [Test]
        public async Task RunDataRefreshCleanup_EnablesDonorUpdateFunction()
        {
            const string donorFunctionsAppName = "donor-functions-app-name";
            const string donorImportFunctionName = "donor-import";
            dataRefreshHistoryRepository.GetInProgressJobs().Returns(new List<DataRefreshRecord> {new DataRefreshRecord()});
            dataRefreshOptions.Value.Returns(new DataRefreshSettings
            {
                DormantDatabaseSize = "Basic",
                DonorFunctionsAppName = donorFunctionsAppName,
                DonorImportFunctionName = donorImportFunctionName
            });

            await dataRefreshCleanupService.RunDataRefreshCleanup();

            await azureFunctionManager.Received().StartFunction(donorFunctionsAppName, donorImportFunctionName);
        }

        [Test]
        public async Task RunDataRefreshCleanup_MarksAllInProgressRecordsAsComplete()
        {
            const int record1Id = 1;
            const int record2Id = 2;
            dataRefreshHistoryRepository.GetInProgressJobs().Returns(new List<DataRefreshRecord>
            {
                new DataRefreshRecord {Id = record1Id},
                new DataRefreshRecord {Id = record2Id},
            });

            await dataRefreshCleanupService.RunDataRefreshCleanup();

            await dataRefreshHistoryRepository.Received().UpdateFinishTime(record1Id, Arg.Any<DateTime>());
            await dataRefreshHistoryRepository.Received().UpdateFinishTime(record2Id, Arg.Any<DateTime>());
        }

        [Test]
        public async Task RunDataRefreshCleanup_MarksAllInProgressRecordsAsFailed()
        {
            const int record1Id = 1;
            const int record2Id = 2;
            dataRefreshHistoryRepository.GetInProgressJobs().Returns(new List<DataRefreshRecord>
            {
                new DataRefreshRecord {Id = record1Id},
                new DataRefreshRecord {Id = record2Id},
            });

            await dataRefreshCleanupService.RunDataRefreshCleanup();

            await dataRefreshHistoryRepository.Received().UpdateSuccessFlag(record1Id, false);
            await dataRefreshHistoryRepository.Received().UpdateSuccessFlag(record2Id, false);
        }

        [Test]
        public async Task SendCleanupRecommendation_WhenJobsInProgress_SendsAlert()
        {
            dataRefreshHistoryRepository.GetInProgressJobs().Returns(new List<DataRefreshRecord> {new DataRefreshRecord()});

            await dataRefreshCleanupService.SendCleanupRecommendation();

            await notificationsClient.Received().SendAlert(Arg.Any<Alert>());
        }

        [Test]
        public async Task SendCleanupRecommendation_WhenNoJobsInProgress_DoesNotSendAlert()
        {
            dataRefreshHistoryRepository.GetInProgressJobs().Returns(new List<DataRefreshRecord>());

            await dataRefreshCleanupService.SendCleanupRecommendation();

            await notificationsClient.DidNotReceive().SendAlert(Arg.Any<Alert>());
        }
    }
}