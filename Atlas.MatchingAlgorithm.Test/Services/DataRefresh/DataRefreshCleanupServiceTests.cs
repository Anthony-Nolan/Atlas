using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Models.AzureManagement;
using Atlas.MatchingAlgorithm.Services.AzureManagement;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Atlas.MatchingAlgorithm.Services.DataRefresh;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Notifications;
using Atlas.MatchingAlgorithm.Settings;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh
{
    [TestFixture]
    public class DataRefreshCleanupServiceTests
    {
        private IMatchingAlgorithmImportLogger logger;
        private IAzureDatabaseNameProvider azureDatabaseNameProvider;
        private IActiveDatabaseProvider activeDatabaseProvider;
        private IAzureDatabaseManager azureDatabaseManager;
        private IDataRefreshHistoryRepository dataRefreshHistoryRepository;
        private IDataRefreshSupportNotificationSender notificationSender;

        private IDataRefreshCleanupService dataRefreshCleanupService;

        [SetUp]
        public void SetUp()
        {
            logger = Substitute.For<IMatchingAlgorithmImportLogger>();
            azureDatabaseNameProvider = Substitute.For<IAzureDatabaseNameProvider>();
            activeDatabaseProvider = Substitute.For<IActiveDatabaseProvider>();
            azureDatabaseManager = Substitute.For<IAzureDatabaseManager>();
            dataRefreshHistoryRepository = Substitute.For<IDataRefreshHistoryRepository>();
            notificationSender = Substitute.For<IDataRefreshSupportNotificationSender>();

            dataRefreshCleanupService = BuildDataRefreshCleanupService();
        }

        [Test]
        public async Task RunDataRefreshCleanup_WhenNoJobsInProgress_DoesNotScaleDatabase()
        {
            dataRefreshHistoryRepository.GetIncompleteRefreshJobs().Returns(new List<DataRefreshRecord>());

            await dataRefreshCleanupService.RunDataRefreshCleanup();

            await azureDatabaseManager.DidNotReceive().UpdateDatabaseSize(Arg.Any<string>(), Arg.Any<AzureDatabaseSize>(), Arg.Any<int?>());
        }

        [Test]
        public async Task RunDataRefreshCleanup_WhenNoJobsInProgress_DoesNotSendRequestManualTeardownNotification()
        {
            dataRefreshHistoryRepository.GetIncompleteRefreshJobs().Returns(new List<DataRefreshRecord>());

            await dataRefreshCleanupService.RunDataRefreshCleanup();

            await notificationSender.DidNotReceive().SendRequestManualTeardownNotification();
        }

        [Test]
        public async Task RunDataRefreshCleanup_ScalesDormantDatabaseToDormantSize()
        {
            const string databaseName = "db-name";
            var dataRefreshSettings = new DataRefreshSettings {DormantDatabaseSize = "S1", DormantDatabaseAutoPauseTimeout = 120};

            dataRefreshHistoryRepository.GetIncompleteRefreshJobs().Returns(new List<DataRefreshRecord> {new DataRefreshRecord()});
            azureDatabaseNameProvider.GetDatabaseName(Arg.Any<TransientDatabase>()).Returns(databaseName);
            dataRefreshCleanupService = BuildDataRefreshCleanupService(dataRefreshSettings);

            await dataRefreshCleanupService.RunDataRefreshCleanup();

            await azureDatabaseManager.Received()
                .UpdateDatabaseSize(databaseName, AzureDatabaseSize.S1, dataRefreshSettings.DormantDatabaseAutoPauseTimeout);
        }

        [Test]
        public async Task RunDataRefreshCleanup_MarksAllInProgressRecordsAsComplete()
        {
            const int record1Id = 1;
            const int record2Id = 2;
            dataRefreshHistoryRepository.GetIncompleteRefreshJobs().Returns(new List<DataRefreshRecord>
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
            dataRefreshHistoryRepository.GetIncompleteRefreshJobs().Returns(new List<DataRefreshRecord>
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
            dataRefreshHistoryRepository.GetIncompleteRefreshJobs().Returns(new List<DataRefreshRecord> {new DataRefreshRecord()});

            await dataRefreshCleanupService.RunDataRefreshCleanup();

            await notificationSender.Received().SendRequestManualTeardownNotification();
        }

        private DataRefreshCleanupService BuildDataRefreshCleanupService(DataRefreshSettings dataRefreshSettings = null)
        {
            var settings = dataRefreshSettings ?? new DataRefreshSettings {DormantDatabaseSize = "S0"};

            return new DataRefreshCleanupService(
                logger,
                azureDatabaseNameProvider,
                activeDatabaseProvider,
                azureDatabaseManager,
                settings,
                dataRefreshHistoryRepository,
                notificationSender);
        }
    }
}