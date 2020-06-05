using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.MultipleAlleleCodeDictionary.MacImportServices;
using Atlas.MultipleAlleleCodeDictionary.MacImportServices.SourceData;
using Atlas.MultipleAlleleCodeDictionary.Test.Integration.Repositories;
using Atlas.MultipleAlleleCodeDictionary.Test.TestHelpers.Builders;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MultipleAlleleCodeDictionary.Test.Integration.IntegrationTests
{
    [TestFixture]
    internal class MacImportTests
    {
        private IMacImporter macImporter;

        private IMacCodeDownloader mockDownloader;
        private ITestMacRepository macRepository;

        [SetUp]
        public void SetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                macImporter = DependencyInjection.DependencyInjection.Provider.GetService<IMacImporter>();
                mockDownloader = DependencyInjection.DependencyInjection.Provider.GetService<IMacCodeDownloader>();
                macRepository = DependencyInjection.DependencyInjection.Provider.GetService<ITestMacRepository>();
            });
        }

        [Test]
        public async Task ImportMacs_InsertsAllMacs()
        {
            const int numberOfMacs = 100;
            // We cannot use LochNessBuilder's "Build(x)" feature as all macs must have unique ids.
            var macs = Enumerable.Range(0, numberOfMacs).Select(i => MacEntityBuilder.New.Build());
            mockDownloader.DownloadAndUnzipStream().Returns(MacSourceFileBuilder.BuildMacFile(macs));
            
            await macImporter.ImportLatestMultipleAlleleCodes();

            var importedMacs = await macRepository.GetAllMacs();
            importedMacs.Count().Should().Be(numberOfMacs);
        }
    }
}