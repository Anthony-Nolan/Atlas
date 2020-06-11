using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
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
        private IActiveHlaNomenclatureVersionAccessor activeVersionAccessor;

        private IDataRefreshRunner dataRefreshRunner;

        [SetUp]
        public async Task SetUp()
        {
            dataRefreshHistoryRepository = DependencyInjection.DependencyInjection.Provider.GetService<ITestDataRefreshHistoryRepository>();
            activeVersionAccessor = DependencyInjection.DependencyInjection.Provider.GetService<IActiveHlaNomenclatureVersionAccessor>();

            dataRefreshRunner = DependencyInjection.DependencyInjection.Provider.GetService<IDataRefreshRunner>();
        }

        [Test]
        public async Task DataRefresh_PopulatesAllStageFlags()
        {
            var expectedStages = EnumExtensions.EnumerateValues<DataRefreshStage>().Except(new[]
            {
                DataRefreshStage.QueuedDonorUpdateProcessing
            });
            
            var refreshRecordId = dataRefreshHistoryRepository.InsertDummySuccessfulRefreshRecord(activeVersionAccessor.GetActiveHlaNomenclatureVersion());

            await dataRefreshRunner.RefreshData(refreshRecordId);

            foreach (var stage in expectedStages)
            {
                var completedTime = await dataRefreshHistoryRepository.GetStageCompletionTime(refreshRecordId, stage);
                completedTime.Should().NotBeNull();
            }
        }
    }
}