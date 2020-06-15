using System.Linq;
using System.Threading.Tasks;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Services.DataRefresh;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Repositories;
using EnumStringValues;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.DataRefresh
{
    [TestFixture]
    internal class DataRefreshRunnerTests
    {
        private ITestDataRefreshHistoryRepository dataRefreshHistoryRepository;

        private IDataRefreshRunner dataRefreshRunner;

        [SetUp]
        public void SetUp()
        {
            dataRefreshHistoryRepository = DependencyInjection.DependencyInjection.Provider.GetService<ITestDataRefreshHistoryRepository>();

            // Set up a dummy record so the refresh does not attempt to regenerate the file-backed metadata dictionary
            dataRefreshHistoryRepository.InsertDummySuccessfulRefreshRecord(Constants.SnapshotHlaNomenclatureVersion);

            dataRefreshRunner = DependencyInjection.DependencyInjection.Provider.GetService<IDataRefreshRunner>();
        }

        [Test]
        public async Task DataRefresh_PopulatesAllStageFlagInOrder()
        {
            var expectedStages = EnumExtensions.EnumerateValues<DataRefreshStage>().Except(new[]
            {
                DataRefreshStage.QueuedDonorUpdateProcessing
            });

            var refreshRecord = new DataRefreshRecord {Database = "DatabaseA", HlaNomenclatureVersion = Constants.SnapshotHlaNomenclatureVersion};
            var refreshRecordId = await dataRefreshHistoryRepository.Create(refreshRecord);

            await dataRefreshRunner.RefreshData(refreshRecordId);

            var completionTimes = await dataRefreshHistoryRepository.GetStageCompletionTimes(refreshRecordId);
            var timesOfExpectedStages = expectedStages.Select(s => completionTimes[s]).ToList();
            timesOfExpectedStages.Should().NotContainNulls();
            timesOfExpectedStages.Should().BeInAscendingOrder();
        }
    }
}