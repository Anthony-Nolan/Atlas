using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Data.Persistent.Models;
using Nova.SearchAlgorithm.Data.Persistent.Repositories;
using Nova.SearchAlgorithm.Services.ConfigurationProviders;
using Nova.SearchAlgorithm.Services.DataRefresh;
using Nova.Utils.ApplicationInsights;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Services.DataRefresh
{
    [TestFixture]
    public class DataRefreshOrchestratorTests
    {
        private ILogger logger;
        private IWmdaHlaVersionProvider wmdaHlaVersionProvider;
        private IDataRefreshService dataRefreshService;
        private IDataRefreshHistoryRepository dataRefreshHistoryRepository;

        private IDataRefreshOrchestrator dataRefreshOrchestrator;

        [SetUp]
        public void SetUp()
        {
            logger = Substitute.For<ILogger>();
            wmdaHlaVersionProvider = Substitute.For<IWmdaHlaVersionProvider>();
            dataRefreshService = Substitute.For<IDataRefreshService>();
            dataRefreshHistoryRepository = Substitute.For<IDataRefreshHistoryRepository>();

            wmdaHlaVersionProvider.GetActiveHlaDatabaseVersion().Returns("old");
            wmdaHlaVersionProvider.GetLatestHlaDatabaseVersion().Returns("new");

            dataRefreshOrchestrator = new DataRefreshOrchestrator(logger, wmdaHlaVersionProvider, dataRefreshService, dataRefreshHistoryRepository);
        }

        [Test]
        public async Task RefreshDataIfNecessary_WhenCurrentWmdaVersionMatchesLatest_DoesNotTriggerDataRefresh()
        {
            const string wmdaVersion = "3330";
            wmdaHlaVersionProvider.GetActiveHlaDatabaseVersion().Returns(wmdaVersion);
            wmdaHlaVersionProvider.GetLatestHlaDatabaseVersion().Returns(wmdaVersion);

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshService.DidNotReceive().RefreshData(Arg.Any<TransientDatabase>(), Arg.Any<string>());
        }

        [Test]
        public async Task RefreshDataIfNecessary_WhenLatestWmdaVersionHigherThanCurrent_TriggersDataRefresh()
        {
            wmdaHlaVersionProvider.GetActiveHlaDatabaseVersion().Returns("3330");
            wmdaHlaVersionProvider.GetLatestHlaDatabaseVersion().Returns("3370");

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshService.Received().RefreshData(Arg.Any<TransientDatabase>(), Arg.Any<string>());
        }

        [Test]
        public async Task RefreshDataIfNecessary_WhenLatestWmdaVersionHigherThanCurrent_AndJobAlreadyInProgress_DoesNotTriggerDataRefresh()
        {
            wmdaHlaVersionProvider.GetActiveHlaDatabaseVersion().Returns("3330");
            wmdaHlaVersionProvider.GetLatestHlaDatabaseVersion().Returns("3370");
            dataRefreshHistoryRepository.GetInProgressJobs().Returns(new List<DataRefreshRecord> {new DataRefreshRecord()});

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshService.DidNotReceive().RefreshData(Arg.Any<TransientDatabase>(), Arg.Any<string>());
        }

        [Test]
        public async Task RefreshDataIfNecessary_TriggersDataRefreshWithLatestWmdaVersion()
        {
            const string latestWmdaVersion = "3370";
            wmdaHlaVersionProvider.GetLatestHlaDatabaseVersion().Returns(latestWmdaVersion);

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshService.Received().RefreshData(Arg.Any<TransientDatabase>(), latestWmdaVersion);
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
            dataRefreshHistoryRepository.GetActiveDatabase().Returns(TransientDatabase.DatabaseA);

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshHistoryRepository.Received().Create(Arg.Is<DataRefreshRecord>(r =>
                r.Database == "DatabaseB"
            ));
        }

        [Test]
        public async Task RefreshDataIfNecessary_WhenDatabaseBActive_StoresRefreshRecordOfDatabaseA()
        {
            dataRefreshHistoryRepository.GetActiveDatabase().Returns(TransientDatabase.DatabaseB);

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshHistoryRepository.Received().Create(Arg.Is<DataRefreshRecord>(r =>
                r.Database == "DatabaseA"
            ));
        }

        [Test]
        public async Task RefreshDataIfNecessary_WhenNoDatabaseActive_StoresRefreshRecordOfDatabaseA()
        {
            dataRefreshHistoryRepository.GetActiveDatabase().Returns((TransientDatabase?) null);

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshHistoryRepository.Received().Create(Arg.Is<DataRefreshRecord>(r =>
                r.Database == "DatabaseA"
            ));
        }

        [Test]
        public async Task RefreshDataIfNecessary_WhenDatabaseAActive_RefreshesDatabaseB()
        {
            dataRefreshHistoryRepository.GetActiveDatabase().Returns(TransientDatabase.DatabaseA);

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshService.RefreshData(TransientDatabase.DatabaseB, Arg.Any<string>());
        }

        [Test]
        public async Task RefreshDataIfNecessary_WhenDatabaseBActive_RefreshesDatabaseA()
        {
            dataRefreshHistoryRepository.GetActiveDatabase().Returns(TransientDatabase.DatabaseB);

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshService.RefreshData(TransientDatabase.DatabaseA, Arg.Any<string>());
        }

        [Test]
        public async Task RefreshDataIfNecessary_WhenNoDatabaseActive_RefreshesDatabaseA()
        {
            dataRefreshHistoryRepository.GetActiveDatabase().Returns((TransientDatabase?) null);

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshService.RefreshData(TransientDatabase.DatabaseA, Arg.Any<string>());
        }

        [Test]
        public async Task RefreshDataIfNecessary_StoresRefreshRecordOfWmdaVersion()
        {
            const string latestWmdaVersion = "latest-wmda";
            wmdaHlaVersionProvider.GetLatestHlaDatabaseVersion().Returns(latestWmdaVersion);

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
            dataRefreshService.RefreshData(Arg.Any<TransientDatabase>(), Arg.Any<string>()).Throws(new Exception(exceptionMessage));

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            logger.Received().SendTrace(Arg.Is<string>(e => e.Contains(exceptionMessage)), LogLevel.Critical);
        }

        [Test]
        public async Task RefreshDataIfNecessary_WhenDataRefreshFails_StoresFinishTime()
        {
            const string exceptionMessage = "something very bad happened";
            dataRefreshService.RefreshData(Arg.Any<TransientDatabase>(), Arg.Any<string>()).Throws(new Exception(exceptionMessage));

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshHistoryRepository.Received().UpdateFinishTime(Arg.Any<int>(), Arg.Any<DateTime>());
        }

        [Test]
        public async Task RefreshDataIfNecessary_WhenDataRefreshFails_StoresSuccessFlagAsFalse()
        {
            const string exceptionMessage = "something very bad happened";
            dataRefreshService.RefreshData(Arg.Any<TransientDatabase>(), Arg.Any<string>()).Throws(new Exception(exceptionMessage));

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshHistoryRepository.Received().UpdateSuccessFlag(Arg.Any<int>(), false);
        }
    }
}