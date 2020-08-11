using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Utils.Http;
using Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders;
using Atlas.HlaMetadataDictionary.WmdaDataAccess;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Models.AzureManagement;
using Atlas.MatchingAlgorithm.Services.AzureManagement;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Atlas.MatchingAlgorithm.Services.DataRefresh;
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
        private IWmdaHlaNomenclatureVersionAccessor wmdaHlaNomenclatureVersionAccessor;
        private IActiveDatabaseProvider activeDatabaseProvider;
        private IDataRefreshRunner dataRefreshRunner;
        private IDataRefreshHistoryRepository dataRefreshHistoryRepository;

        private IAzureDatabaseManager azureDatabaseManager;
        private IDataRefreshNotificationSender dataRefreshNotificationSender;

        private IDataRefreshOrchestrator dataRefreshOrchestrator;
        private const string ExistingHlaVersion = "old";
        private const string NewHlaVersion = "new";

        [SetUp]
        public void SetUp()
        {
            wmdaHlaNomenclatureVersionAccessor = Substitute.For<IWmdaHlaNomenclatureVersionAccessor>();

            logger = Substitute.For<IMatchingAlgorithmImportLogger>();
            activeDatabaseProvider = Substitute.For<IActiveDatabaseProvider>();
            dataRefreshRunner = Substitute.For<IDataRefreshRunner>();
            dataRefreshHistoryRepository = Substitute.For<IDataRefreshHistoryRepository>();
            azureDatabaseManager = Substitute.For<IAzureDatabaseManager>();
            dataRefreshNotificationSender = Substitute.For<IDataRefreshNotificationSender>();

            dataRefreshOrchestrator = BuildDataRefreshOrchestrator();
        }

        private DataRefreshOrchestrator BuildDataRefreshOrchestrator(DataRefreshSettings dataRefreshSettings = null)
        {
            var hlaMetadataDictionaryBuilder = new HlaMetadataDictionaryBuilder().Using(wmdaHlaNomenclatureVersionAccessor);
            
            var activeHlaVersionAccessor = Substitute.For<IActiveHlaNomenclatureVersionAccessor>();
            activeHlaVersionAccessor.DoesActiveHlaNomenclatureVersionExist().Returns(true);
            activeHlaVersionAccessor.GetActiveHlaNomenclatureVersion().Returns(ExistingHlaVersion);

            var settings = dataRefreshSettings ?? DataRefreshSettingsBuilder.New.Build();
            
            return new DataRefreshOrchestrator(
                logger,
                settings,
                hlaMetadataDictionaryBuilder,
                activeHlaVersionAccessor,
                activeDatabaseProvider,
                dataRefreshRunner,
                dataRefreshHistoryRepository,
                azureDatabaseManager,
                new AzureDatabaseNameProvider(settings),
                dataRefreshNotificationSender
            );
        }

        [Test]
        public async Task RefreshDataIfNecessary_WhenActiveHlaVersionMatchesLatest_DoesNotTriggerDataRefresh()
        {
            wmdaHlaNomenclatureVersionAccessor.GetLatestStableHlaNomenclatureVersion().ReturnsForAnyArgs(ExistingHlaVersion);

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshRunner.DidNotReceiveWithAnyArgs().RefreshData(default);
        }

        [Test]
        public async Task RefreshDataIfNecessary_WhenActiveHlaVersionMatchesLatest_AndShouldForceRefresh_TriggersDataRefresh()
        {
            wmdaHlaNomenclatureVersionAccessor.GetLatestStableHlaNomenclatureVersion().ReturnsForAnyArgs(ExistingHlaVersion);

            await dataRefreshOrchestrator.RefreshDataIfNecessary(shouldForceRefresh: true);

            await dataRefreshRunner.ReceivedWithAnyArgs().RefreshData(default);
        }

        [Test]
        public async Task RefreshDataIfNecessary_WhenLatestHlaVersionHigherThanCurrent_TriggersDataRefresh()
        {
            wmdaHlaNomenclatureVersionAccessor.GetLatestStableHlaNomenclatureVersion().ReturnsForAnyArgs(NewHlaVersion);

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshRunner.ReceivedWithAnyArgs().RefreshData(default);
        }

        [Test]
        public async Task RefreshDataIfNecessary_WhenLatestHlaVersionHigherThanCurrent_AndJobAlreadyInProgress_DoesNotTriggerDataRefresh()
        {
            wmdaHlaNomenclatureVersionAccessor.GetLatestStableHlaNomenclatureVersion().ReturnsForAnyArgs(NewHlaVersion);
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
            wmdaHlaNomenclatureVersionAccessor.GetLatestStableHlaNomenclatureVersion().ReturnsForAnyArgs(NewHlaVersion);

            await dataRefreshOrchestrator.RefreshDataIfNecessary();
            await dataRefreshHistoryRepository.Received().Create(Arg.Is<DataRefreshRecord>(r => string.IsNullOrWhiteSpace(r.HlaNomenclatureVersion)));
        }

        [Test]
        public async Task RefreshDataIfNecessary_EventuallyRecordsDataRefreshOccurredWithLatestWmdaVersion()
        {
            wmdaHlaNomenclatureVersionAccessor.GetLatestStableHlaNomenclatureVersion().ReturnsForAnyArgs(NewHlaVersion);
            dataRefreshRunner.RefreshData(Arg.Any<int>()).Returns(NewHlaVersion);

            await dataRefreshOrchestrator.RefreshDataIfNecessary();
            await dataRefreshHistoryRepository.Received().UpdateExecutionDetails(Arg.Any<int>(), NewHlaVersion, Arg.Any<DateTime?>());
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
        public async Task RefreshData_ScalesActiveDatabaseToDormantSize()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DatabaseAName, "db-a")
                .With(s => s.DormantDatabaseSize, "S0")
                .Build();
            dataRefreshOrchestrator = BuildDataRefreshOrchestrator(settings);
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
            dataRefreshOrchestrator = BuildDataRefreshOrchestrator(settings);
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
            dataRefreshOrchestrator = BuildDataRefreshOrchestrator(settings);
            activeDatabaseProvider.GetActiveDatabase().Returns(TransientDatabase.DatabaseA);
            dataRefreshRunner.RefreshData(Arg.Any<int>()).Throws(new Exception());

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await azureDatabaseManager.DidNotReceive().UpdateDatabaseSize(settings.DatabaseAName, AzureDatabaseSize.S0);
        }

        [Test]
        public async Task RefreshData_SendsNotificationOnInitialisation()
        {
            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshNotificationSender.ReceivedWithAnyArgs().SendInitialisationNotification(default);
        }

        [Test]
        public async Task RefreshData_SendsNotificationOnSuccess()
        {
            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshNotificationSender.ReceivedWithAnyArgs().SendSuccessNotification(default);
        }

        [Test]
        public async Task RefreshData_SendsAlertOnFailure()
        {
            dataRefreshRunner.RefreshData(Arg.Any<int>()).Throws(new Exception());

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshNotificationSender.ReceivedWithAnyArgs().SendFailureAlert(default);
        }

        [Test]
        public async Task ContinueDataRefresh_WhenNoJobsInProgress_ThrowsException()
        {
            dataRefreshHistoryRepository.GetInProgressJobs().Returns(new List<DataRefreshRecord>());

            await dataRefreshOrchestrator.Invoking(r => r.ContinueDataRefresh()).Should().ThrowAsync<AtlasHttpException>();
        }

        [Test]
        public async Task ContinueDataRefresh_WhenOneJobStarted_DoesNotThrow()
        {
            var record = DataRefreshRecordBuilder.New.Build();
            dataRefreshHistoryRepository.GetInProgressJobs().Returns(new[]{ record });

            await dataRefreshOrchestrator.Invoking(r => r.ContinueDataRefresh()).Should().NotThrowAsync();
        }

        [Test]
        public async Task ContinueDataRefresh_WhenOneJobPreviouslyContinuedButIncomplete_DoesNotThrow()
        {
            var record = DataRefreshRecordBuilder.New.With(r => r.RefreshContinueUtc, DateTime.UtcNow).Build();
            dataRefreshHistoryRepository.GetInProgressJobs().Returns(new[] { record });

            await dataRefreshOrchestrator.Invoking(r => r.ContinueDataRefresh()).Should().NotThrowAsync();
        }

        [Test]
        public async Task ContinueDataRefresh_WithMultipleJobsInProgress_ThrowsException()
        {
            dataRefreshHistoryRepository.GetInProgressJobs().Returns(DataRefreshRecordBuilder.New.Build(2));

            await dataRefreshOrchestrator.Invoking(r => r.ContinueDataRefresh()).Should().ThrowAsync<AtlasHttpException>();
        }

        [Test]
        public async Task ContinueDataRefresh_WithSingleJobInProgress_TriggersRefresh()
        {
            var record = DataRefreshRecordBuilder.New.With(r => r.Id, 19).Build();
            dataRefreshHistoryRepository.GetInProgressJobs().Returns(new[] {record});

            await dataRefreshOrchestrator.ContinueDataRefresh();

            await dataRefreshRunner.Received().RefreshData(record.Id);
        }
    }
}