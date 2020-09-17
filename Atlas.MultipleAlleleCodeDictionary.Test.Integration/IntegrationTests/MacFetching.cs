using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Repositories;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MultipleAlleleCodeDictionary.Test.Integration.IntegrationTests
{
    [TestFixture]
    internal class MacFetching
    {
        private IMacRepository macRepository;

        [SetUp]
        public void SetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                macRepository = DependencyInjection.DependencyInjection.Provider.GetService<IMacRepository>();
            });
        }

        [Test]
        public async Task GetMac_WhenMacNotFound_ThrowsUsefulException()
        {
            await macRepository.Invoking(r => r.GetMac("THIS IS NOT RECOGNISED")).Should().ThrowAsync<MacNotFoundException>();
        }
    }
}