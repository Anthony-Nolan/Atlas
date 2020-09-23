using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Utils.Http;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Models.AzureManagement;
using Atlas.MatchingAlgorithm.Services.AzureManagement;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Atlas.MatchingAlgorithm.Services.DataRefresh;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Notifications;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.DataRefresh;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh
{
    [TestFixture]
    public class DataRefreshOrchestratorTests
    {
        private IMatchingAlgorithmImportLogger logger;
        private IActiveDatabaseProvider activeDatabaseProvider;
        private IDataRefreshRunner dataRefreshRunner;
        private IDataRefreshHistoryRepository dataRefreshHistoryRepository;

        private IAzureDatabaseManager azureDatabaseManager;
        private IDataRefreshSupportNotificationSender dataRefreshSupportNotificationSender;
        private IDataRefreshCompletionNotifier dataRefreshCompletionNotifier;

        private IDataRefreshOrchestrator dataRefreshOrchestrator;
        private const string ExistingHlaVersion = "old";
        private const string NewHlaVersion = "new";
        private const int DefaultRecordId = 123;

        [SetUp]
        public void SetUp()
        {
            logger = Substitute.For<IMatchingAlgorithmImportLogger>();
            activeDatabaseProvider = Substitute.For<IActiveDatabaseProvider>();
            dataRefreshRunner = Substitute.For<IDataRefreshRunner>();
            dataRefreshHistoryRepository = Substitute.For<IDataRefreshHistoryRepository>();
            azureDatabaseManager = Substitute.For<IAzureDatabaseManager>();
            dataRefreshSupportNotificationSender = Substitute.For<IDataRefreshSupportNotificationSender>();
            dataRefreshCompletionNotifier = Substitute.For<IDataRefreshCompletionNotifier>();

            dataRefreshOrchestrator = BuildDataRefreshOrchestrator();

            var record = DataRefreshRecordBuilder.New
                .With(r => r.Id, DefaultRecordId)
                .Build();
            dataRefreshHistoryRepository.GetIncompleteRefreshJobs().Returns(new[] { record });
        }

        private DataRefreshOrchestrator BuildDataRefreshOrchestrator(DataRefreshSettings dataRefreshSettings = null)
        {
            var activeHlaVersionAccessor = Substitute.For<IActiveHlaNomenclatureVersionAccessor>();
            activeHlaVersionAccessor.DoesActiveHlaNomenclatureVersionExist().Returns(true);
            activeHlaVersionAccessor.GetActiveHlaNomenclatureVersion().Returns(ExistingHlaVersion);

            var settings = dataRefreshSettings ?? DataRefreshSettingsBuilder.New.Build();
            
            return new DataRefreshOrchestrator(
                logger,
                settings,
                activeDatabaseProvider,
                dataRefreshRunner,
                dataRefreshHistoryRepository,
                azureDatabaseManager,
                new AzureDatabaseNameProvider(settings),
                dataRefreshSupportNotificationSender,
                dataRefreshCompletionNotifier
            );
        }

        [Test]
        public async Task OrchestrateDataRefresh_WhenNoIncompleteJobs_ThrowsException()
        {
            dataRefreshHistoryRepository.GetIncompleteRefreshJobs().Returns(new List<DataRefreshRecord>());

            await dataRefreshOrchestrator.Invoking(r => r.OrchestrateDataRefresh(0)).Should().ThrowAsync<AtlasHttpException>();
        }

        [Test]
        public async Task OrchestrateDataRefresh_WithMultipleIncompleteJobs_ThrowsException()
        {
            dataRefreshHistoryRepository.GetIncompleteRefreshJobs().Returns(DataRefreshRecordBuilder.New.Build(2));

            await dataRefreshOrchestrator.Invoking(r => r.OrchestrateDataRefresh(0)).Should().ThrowAsync<AtlasHttpException>();
        }

        [Test]
        public async Task OrchestrateDataRefresh_SendsNotification()
        {
            const int recordId = 20;
            const int currentAttemptNumber = 2;

            var record = DataRefreshRecordBuilder.New
                .With(r => r.Id, recordId)
                .With(r => r.RefreshAttemptedCount, currentAttemptNumber - 1)
                .Build();
            dataRefreshHistoryRepository.GetIncompleteRefreshJobs().Returns(new[] { record });

            await dataRefreshOrchestrator.OrchestrateDataRefresh(recordId);

            await dataRefreshSupportNotificationSender.ReceivedWithAnyArgs().SendInProgressNotification(recordId, currentAttemptNumber);
        }

        [Test]
        public async Task OrchestrateDataRefresh_UpdatesRunAttemptDetails()
        {
            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await dataRefreshHistoryRepository.Received().UpdateRunAttemptDetails(DefaultRecordId);
        }

        [Test]
        public async Task OrchestrateDataRefresh_TriggersRefresh()
        {
            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await dataRefreshRunner.Received().RefreshData(DefaultRecordId);
        }

        [Test]
        public async Task OrchestrateDataRefresh_EventuallyRecordsDataRefreshOccurredWithLatestWmdaVersion()
        {
            dataRefreshRunner.RefreshData(Arg.Any<int>()).Returns(NewHlaVersion);

            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);
            await dataRefreshHistoryRepository.Received().UpdateExecutionDetails(DefaultRecordId, NewHlaVersion, Arg.Any<DateTime?>());
        }

        [Test]
        public async Task OrchestrateDataRefresh_WhenJobSuccessful_StoresRecordAsSuccess()
        {
            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await dataRefreshHistoryRepository.Received().UpdateSuccessFlag(DefaultRecordId, true);
        }

        [Test]
        public async Task OrchestrateDataRefresh_WhenJobSuccessful_UpdatesExecutionDetails()
        {
            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await dataRefreshHistoryRepository.ReceivedWithAnyArgs().UpdateExecutionDetails(default, default, default);
        }

        [Test]
        public async Task OrchestrateDataRefresh_WhenDataRefreshFails_LogsExceptionDetails()
        {
            const string exceptionMessage = "something very bad happened";
            dataRefreshRunner.RefreshData(Arg.Any<int>()).Throws(new Exception(exceptionMessage));

            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            logger.Received().SendTrace(Arg.Is<string>(e => e.Contains(exceptionMessage)), LogLevel.Critical);
        }

        [Test]
        public async Task OrchestrateDataRefresh_WhenDataRefreshFails_UpdatesExecutionDetails()
        {
            const string exceptionMessage = "something very bad happened";
            dataRefreshRunner.RefreshData(Arg.Any<int>()).Throws(new Exception(exceptionMessage));

            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await dataRefreshHistoryRepository.ReceivedWithAnyArgs().UpdateExecutionDetails(default, default, default);
        }

        [Test]
        public async Task OrchestrateDataRefresh_WhenDataRefreshFails_StoresSuccessFlagAsFalse()
        {
            const string exceptionMessage = "something very bad happened";
            dataRefreshRunner.RefreshData(Arg.Any<int>()).Throws(new Exception(exceptionMessage));

            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await dataRefreshHistoryRepository.Received().UpdateSuccessFlag(DefaultRecordId, false);
        }

        [Test]
        public async Task RefreshData_ScalesActiveDatabaseToDormantSize()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DatabaseAName, "db-a")
                .With(s => s.DormantDatabaseSize, "S0")
                .Build();
            dataRefreshOrchestrator = BuildDataRefreshOrchestrator(settings);
            activeDatabaseProvider.GetActiveDatabase().Returns(TransientDatabase.DatabaseA);

            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await azureDatabaseManager.Received().UpdateDatabaseSize(settings.DatabaseAName, AzureDatabaseSize.S0);
        }

        [Test]
        public async Task RefreshData_ScalesDownDatabaseThatWasActiveWhenTheJobStarted()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DatabaseAName, "db-a")
                .With(s => s.DormantDatabaseSize, "S0")
                .Build();
            dataRefreshOrchestrator = BuildDataRefreshOrchestrator(settings);
            activeDatabaseProvider.GetActiveDatabase().Returns(TransientDatabase.DatabaseA);

            // Marking refresh record as complete will switch over which database is considered "active". Emulating this with mocks here.
            dataRefreshHistoryRepository.WhenForAnyArgs(r => r.UpdateSuccessFlag(0, true)).Do(x =>
            {
                activeDatabaseProvider.GetActiveDatabase().Returns(TransientDatabase.DatabaseB);
            });

            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await azureDatabaseManager.Received().UpdateDatabaseSize(settings.DatabaseAName, AzureDatabaseSize.S0);
        }

        [Test]
        public async Task RefreshData_WhenRefreshFails_DoesNotScaleActiveDatabaseToDormantSize()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DatabaseAName, "db-a")
                .With(s => s.DormantDatabaseSize, "S0")
                .Build();
            dataRefreshOrchestrator = BuildDataRefreshOrchestrator(settings);
            activeDatabaseProvider.GetActiveDatabase().Returns(TransientDatabase.DatabaseA);
            dataRefreshRunner.RefreshData(Arg.Any<int>()).Throws(new Exception());

            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await azureDatabaseManager.DidNotReceive().UpdateDatabaseSize(settings.DatabaseAName, AzureDatabaseSize.S0);
        }

        [Test]
        public async Task RefreshData_NotifiesOnSuccess()
        {
            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await dataRefreshCompletionNotifier.ReceivedWithAnyArgs().NotifyOfSuccess(default);
        }

        [Test]
        public async Task RefreshData_NotifiesOnFailure()
        {
            dataRefreshRunner.RefreshData(Arg.Any<int>()).Throws(new Exception());

            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await dataRefreshCompletionNotifier.ReceivedWithAnyArgs().NotifyOfFailure(default);
        }
    }
}