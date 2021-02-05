using Atlas.MatchPrediction.Test.Integration.DependencyInjection;
using Atlas.MatchPrediction.Test.Integration.TestHelpers;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests
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
            // DatabaseManager.ClearDatabases();
        }
    }
}