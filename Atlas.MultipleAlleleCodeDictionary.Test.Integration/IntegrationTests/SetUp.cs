using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Repositories;
using Atlas.MultipleAlleleCodeDictionary.Test.Integration.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MultipleAlleleCodeDictionary.Test.Integration.IntegrationTests
{
    [SetUpFixture]
    public class IntegrationTestSetUp
    {
        [OneTimeSetUp]
        public async Task Setup()
        {
            await TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage_Async(async () =>
            {
                DependencyInjection.DependencyInjection.Provider = ServiceConfiguration.CreateProvider();
                await ResetDatabase();
            });
        }

        private static async Task ResetDatabase()
        {
            var macRepository = DependencyInjection.DependencyInjection.Provider.GetService<IMacRepository>();
            await macRepository.TruncateMacTable();
        }
    }
}