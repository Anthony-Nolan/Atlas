using Atlas.Common.Test.SharedTestHelpers;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.DependencyInjection;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests
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
            });
        }
    }
}