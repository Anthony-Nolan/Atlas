using Microsoft.Extensions.DependencyInjection;
using Atlas.MatchingAlgorithm.Test.Integration.DependencyInjection;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests
{
    [SetUpFixture]
    public class IntegrationTestSetUp
    {
        [OneTimeSetUp]
        public void Setup()
        {
            DependencyInjection.DependencyInjection.Provider = ServiceConfiguration.CreateProvider();
            ResetDatabase();
        }

        private static void ResetDatabase()
        {
            DatabaseManager.SetupDatabase();
            DatabaseManager.ClearDatabases();
        }
    }
}