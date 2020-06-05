using Atlas.Common.Test.SharedTestHelpers;
using Atlas.MultipleAlleleCodeDictionary.Test.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MultipleAlleleCodeDictionary.Test.UnitTests
{
    [SetUpFixture]
    public class UnitTestSetUp
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