using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Services.DataRefresh;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Repositories;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.DataRefresh;
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
            dataRefreshHistoryRepository?.InsertDummySuccessfulRefreshRecord(FileBackedHlaMetadataRepositoryBaseReader.OlderTestHlaVersion);

            dataRefreshRunner = DependencyInjection.DependencyInjection.Provider.GetService<IDataRefreshRunner>();
        }

        [TearDown]
        public void TearDown()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(IntegrationTestSetUp.RunInitialDataRefresh);
        }

        [Test]
        public async Task DataRefresh_WhenDatabaseHasNoRefreshRecords_DoesNotThrow()
        {
            await dataRefreshHistoryRepository.RemoveAllDataRefreshRecords();
            var refreshRecordId = await dataRefreshHistoryRepository.Create(DataRefreshRecordBuilder.New.Build());

            await dataRefreshRunner.Invoking(r => r.RefreshData(refreshRecordId)).Should().NotThrowAsync();
        }
        
        [Test]
        public async Task DataRefresh_PopulatesAllStageFlagInOrder()
        {
            var expectedStages = EnumExtensions.EnumerateValues<DataRefreshStage>().Except(new[]
            {
                DataRefreshStage.QueuedDonorUpdateProcessing
            });

            var refreshRecord = new DataRefreshRecord {Database = "DatabaseA"};
            var refreshRecordId = await dataRefreshHistoryRepository.Create(refreshRecord);

            await dataRefreshRunner.RefreshData(refreshRecordId);

            var completionTimes = await dataRefreshHistoryRepository.GetStageCompletionTimes(refreshRecordId);
            var timesOfExpectedStages = expectedStages.Select(s => completionTimes[s]).ToList();
            timesOfExpectedStages.Should().NotContainNulls();
            timesOfExpectedStages.Should().BeInAscendingOrder();
        }
    }
}