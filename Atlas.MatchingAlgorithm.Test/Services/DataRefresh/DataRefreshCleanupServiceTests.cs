using Microsoft.Extensions.Options;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Models.AzureManagement;
using Atlas.MatchingAlgorithm.Services.AzureManagement;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Atlas.MatchingAlgorithm.Services.DataRefresh;
using Atlas.MatchingAlgorithm.ConfigSettings;
using Atlas.Utils.Core.ApplicationInsights;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh
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
        private IDataRefreshNotificationSender notificationSender;

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
            notificationSender = Substitute.For<IDataRefreshNotificationSender>();

            dataRefreshOptions.Value.Returns(new DataRefreshSettings { DormantDatabaseSize = "S0" });

            dataRefreshCleanupService = new DataRefreshCleanupService(
                logger,
                azureDatabaseNameProvider,
                activeDatabaseProvider,
                azureDatabaseManager,
                dataRefreshOptions,
                azureFunctionManager,
                dataRefreshHistoryRepository,
                notificationSender);
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
        public async Task RunDataRefreshCleanup_WhenNoJobsInProgress_DoesNotSendRequestManualTeardownNotification()
        {
            dataRefreshHistoryRepository.GetInProgressJobs().Returns(new List<DataRefreshRecord>());

            await dataRefreshCleanupService.RunDataRefreshCleanup();

            await notificationSender.DidNotReceive().SendRequestManualTeardownNotification();
        }

        [Test]
        public async Task RunDataRefreshCleanup_ScalesDormantDatabaseToDormantSize()
        {
            const string databaseName = "db-name";
            dataRefreshHistoryRepository.GetInProgressJobs().Returns(new List<DataRefreshRecord> { new DataRefreshRecord() });
            azureDatabaseNameProvider.GetDatabaseName(Arg.Any<TransientDatabase>()).Returns(databaseName);
            dataRefreshOptions.Value.Returns(new DataRefreshSettings { DormantDatabaseSize = "S1" });

            await dataRefreshCleanupService.RunDataRefreshCleanup();

            await azureDatabaseManager.Received().UpdateDatabaseSize(databaseName, AzureDatabaseSize.S1);
        }

        [Test]
        public async Task RunDataRefreshCleanup_EnablesDonorUpdateFunction()
        {
            const string donorFunctionsAppName = "donor-functions-app-name";
            const string donorImportFunctionName = "donor-import";
            dataRefreshHistoryRepository.GetInProgressJobs().Returns(new List<DataRefreshRecord> { new DataRefreshRecord() });
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

            await dataRefreshHistoryRepository.Received().UpdateExecutionDetails(record1Id, Arg.Any<string>(), Arg.Any<DateTime?>());
            await dataRefreshHistoryRepository.Received().UpdateExecutionDetails(record2Id, Arg.Any<string>(), Arg.Any<DateTime?>());
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
        public async Task RunDataRefreshCleanup_SendsRequestManualTeardownNotification()
        {
            dataRefreshHistoryRepository.GetInProgressJobs().Returns(new List<DataRefreshRecord> { new DataRefreshRecord() });

            await dataRefreshCleanupService.RunDataRefreshCleanup();

            await notificationSender.Received().SendRequestManualTeardownNotification();
        }

        [Test]
        public async Task SendCleanupRecommendation_WhenJobsInProgress_SendsAlert()
        {
            dataRefreshHistoryRepository.GetInProgressJobs().Returns(new List<DataRefreshRecord> { new DataRefreshRecord() });

            await dataRefreshCleanupService.SendCleanupRecommendation();

            await notificationSender.Received().SendRecommendManualCleanupAlert();
        }

        [Test]
        public async Task SendCleanupRecommendation_WhenNoJobsInProgress_DoesNotSendAlert()
        {
            dataRefreshHistoryRepository.GetInProgressJobs().Returns(new List<DataRefreshRecord>());

            await dataRefreshCleanupService.SendCleanupRecommendation();

            await notificationSender.DidNotReceive().SendRecommendManualCleanupAlert();
        }
    }
}