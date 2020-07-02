using Atlas.Common.Test.SharedTestHelpers;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests;
using Atlas.MatchingAlgorithm.Test.Integration.DependencyInjection;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Repositories;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests
{
    [SetUpFixture]
    public class IntegrationTestSetUp
    {
        [OneTimeSetUp]
        public void Setup()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                DependencyInjection.DependencyInjection.Provider = ServiceConfiguration.CreateProvider();
                ResetDatabase();
                RunInitialDataRefresh();
            });
        }

        /// <summary>
        /// Sets up a data refresh record to ensure a hla nomenclature version is available, if running only non-data refresh tests.
        /// </summary>
        private static void RunInitialDataRefresh()
        {
            var dataRefreshHistoryRepository = DependencyInjection.DependencyInjection.Provider.GetService<ITestDataRefreshHistoryRepository>();
            dataRefreshHistoryRepository.InsertDummySuccessfulRefreshRecord(Constants.SnapshotHlaNomenclatureVersion);
        }

        private static void ResetDatabase()
        {
            DatabaseManager.MigrateDatabases();
            DatabaseManager.ClearDatabases();
        }
    }
}