using Atlas.Common.Test.SharedTestHelpers;
using Atlas.DonorImport.Test.Integration.DependencyInjection;
using Atlas.DonorImport.Test.Integration.TestHelpers;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Integration.IntegrationTests
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
            });
        }

        private static void ResetDatabase()
        {
            DatabaseManager.SetupDatabase();
            DatabaseManager.ClearDatabases();
        }
    }
}