using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;
using Atlas.MultipleAlleleCodeDictionary.Services.MacImportServices.SourceData;
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

        [TearDown]
        public async Task TearDown()
        {
            await macRepository.DeleteAllMacs();
        }

        [Test]
        public async Task ImportMacs_InsertsAllMacs()
        {
            const int numberOfMacs = 3;
            // We cannot use LochNessBuilder's "Build(x)" feature as all macs must have unique ids.
            var macs = Enumerable.Range(0, numberOfMacs).Select(i => MacBuilder.New.Build());
            mockDownloader.DownloadAndUnzipStream().Returns(MacSourceFileBuilder.BuildMacFile(macs));

            await macImporter.ImportLatestMacs();

            var importedMacs = await macRepository.GetAllMacs();
            importedMacs.Count().Should().Be(numberOfMacs);
        }
        
        [Test]
        public async Task ImportMacs_WithMoreThanOneBatchOfMacs_InsertsAllMacs()
        {
            // Max batch size for inserting to cloud storage is 100, so regardless of the batch size this will always be >1 batch
            const int numberOfMacs = 101;
            // We cannot use LochNessBuilder's "Build(x)" feature as all macs must have unique ids.
            var macs = Enumerable.Range(0, numberOfMacs).Select(i => MacBuilder.New.Build());
            mockDownloader.DownloadAndUnzipStream().Returns(MacSourceFileBuilder.BuildMacFile(macs));

            await macImporter.ImportLatestMacs();

            var importedMacs = await macRepository.GetAllMacs();
            importedMacs.Count().Should().Be(numberOfMacs);
        }

        [Test]
        public async Task ImportMacs_WithExistingMacs_InsertsNewMacs()
        {
            const int numberOfMacs = 2;
            var macs = MacBuilder.New.Build(numberOfMacs).ToList();
            mockDownloader.DownloadAndUnzipStream().Returns(MacSourceFileBuilder.BuildMacFile(macs));
            await macImporter.ImportLatestMacs();

            const int numberOfNewMacs = 2;
            var newMacs = MacBuilder.New.Build(numberOfNewMacs).ToList();
            mockDownloader.DownloadAndUnzipStream().Returns(MacSourceFileBuilder.BuildMacFile(macs.Concat(newMacs)));
            await macImporter.ImportLatestMacs();

            var importedMacs = await macRepository.GetAllMacs();
            importedMacs.Count().Should().Be(numberOfMacs + numberOfNewMacs);
        }
        
        [Test]
        public async Task ImportMacs_WithNoNewMacs_DoesNotInsertAnyMacs()
        {
            const int numberOfMacs = 2;
            var macs = MacBuilder.New.Build(numberOfMacs).ToList();
            mockDownloader.DownloadAndUnzipStream().Returns(MacSourceFileBuilder.BuildMacFile(macs));
            await macImporter.ImportLatestMacs();

            mockDownloader.DownloadAndUnzipStream().Returns(MacSourceFileBuilder.BuildMacFile(macs));
            await macImporter.ImportLatestMacs();

            var importedMacs = await macRepository.GetAllMacs();
            importedMacs.Count().Should().Be(numberOfMacs);
        }

        [Test]
        public async Task ImportMacs_WithMacsOfDifferentLength_DoesNotReImportAnyMacs()
        {
            var shorterEarlyMac = MacBuilder.New.With(m => m.Code, "AA");
            var shorterLateMac = MacBuilder.New.With(m => m.Code, "ZZ");
            var longerEarlyMac = MacBuilder.New.With(m => m.Code, "AAA");
            var longerLateMac = MacBuilder.New.With(m => m.Code, "AAZ");

            mockDownloader.DownloadAndUnzipStream().Returns(
                MacSourceFileBuilder.BuildMacFile(shorterEarlyMac, shorterLateMac, longerEarlyMac));
            await macImporter.ImportLatestMacs();

            mockDownloader.DownloadAndUnzipStream().Returns(
                MacSourceFileBuilder.BuildMacFile(shorterEarlyMac, shorterLateMac, longerEarlyMac, longerLateMac));
            await macImporter.ImportLatestMacs();

            var importedMacs = await macRepository.GetAllMacs();
            importedMacs.Count().Should().Be(4);
        }
    }
}