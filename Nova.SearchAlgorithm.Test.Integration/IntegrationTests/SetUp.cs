using Microsoft.Extensions.DependencyInjection;
using Nova.SearchAlgorithm.Test.Integration.DependencyInjection;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Integration.IntegrationTests
{
    [SetUpFixture]
    public class IntegrationTestSetUp
    {
        [OneTimeSetUp]
        public void Setup()
        {
            DependencyInjection.DependencyInjection.Provider = ServiceModule.CreateProvider();
            DependencyInjection.DependencyInjection.Provider.GetService<IStorageEmulator>().Start();
            ResetDatabase();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            DependencyInjection.DependencyInjection.Provider.GetService<IStorageEmulator>().Stop();
        }

        private static void ResetDatabase()
        {
            DatabaseManager.SetupDatabase();
            DatabaseManager.ClearDatabase();
        }
    }
}