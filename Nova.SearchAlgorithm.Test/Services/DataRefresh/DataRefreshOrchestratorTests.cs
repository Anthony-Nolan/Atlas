using System.Collections.Generic;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Data.Persistent.Models;
using Nova.SearchAlgorithm.Data.Persistent.Repositories;
using Nova.SearchAlgorithm.Services.ConfigurationProviders;
using Nova.SearchAlgorithm.Services.DataRefresh;
using NSubstitute;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Services.DataRefresh
{
    [TestFixture]
    public class DataRefreshOrchestratorTests
    {
        private IWmdaHlaVersionProvider wmdaHlaVersionProvider;
        private IDataRefreshService dataRefreshService;
        private IDataRefreshHistoryRepository dataRefreshHistoryRepository;

        private IDataRefreshOrchestrator dataRefreshOrchestrator;

        [SetUp]
        public void SetUp()
        {
            wmdaHlaVersionProvider = Substitute.For<IWmdaHlaVersionProvider>();
            dataRefreshService = Substitute.For<IDataRefreshService>();
            dataRefreshHistoryRepository = Substitute.For<IDataRefreshHistoryRepository>();

            dataRefreshOrchestrator = new DataRefreshOrchestrator(wmdaHlaVersionProvider, dataRefreshService, dataRefreshHistoryRepository);
        }

        [Test]
        public async Task RefreshDataIfNecessary_WhenCurrentWmdaVersionMatchesLatest_DoesNotTriggerDataRefresh()
        {
            const string wmdaVersion = "3330";
            wmdaHlaVersionProvider.GetActiveHlaDatabaseVersion().Returns(wmdaVersion);
            wmdaHlaVersionProvider.GetLatestHlaDatabaseVersion().Returns(wmdaVersion);

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshService.DidNotReceive().RefreshData();
        }

        [Test]
        public async Task RefreshDataIfNecessary_WhenLatestWmdaVersionHigherThanCurrent_TriggersDataRefresh()
        {
            wmdaHlaVersionProvider.GetActiveHlaDatabaseVersion().Returns("3330");
            wmdaHlaVersionProvider.GetLatestHlaDatabaseVersion().Returns("3370");

            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshService.Received().RefreshData();
        }

        [Test]
        public async Task RefreshDataIfNecessary_WhenLatestWmdaVersionHigherThanCurrent_AndJobAlreadyInProgress_DoesNotTriggerDataRefresh()
        {
            wmdaHlaVersionProvider.GetActiveHlaDatabaseVersion().Returns("3330");
            wmdaHlaVersionProvider.GetLatestHlaDatabaseVersion().Returns("3370");
            dataRefreshHistoryRepository.GetInProgressJobs().Returns(new List<DataRefreshRecord> {new DataRefreshRecord()});
            
            await dataRefreshOrchestrator.RefreshDataIfNecessary();

            await dataRefreshService.DidNotReceive().RefreshData();
        }
    }
}