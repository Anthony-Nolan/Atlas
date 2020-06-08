using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Models;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Repositories;
using Atlas.MultipleAlleleCodeDictionary.MacImportServices;
using Atlas.MultipleAlleleCodeDictionary.MacImportServices.SourceData;
using Atlas.MultipleAlleleCodeDictionary.Test.TestHelpers.Builders;
using Atlas.MultipleAlleleCodeDictionary.utils;
using Atlas.Common.ApplicationInsights;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MultipleAlleleCodeDictionary.Test.UnitTests
{
    [TestFixture]
    internal class MacImporterTests
    {
        private IMacImporter macImporter;
        private IMacCodeDownloader mockDownloader;
        private IMacRepository mockRepository;
        private IMacParser macParser;
        private ILogger mockLogger;

        [SetUp]
        public void Setup()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                mockDownloader = Substitute.For<IMacCodeDownloader>();
                mockRepository = Substitute.For<IMacRepository>();
                mockLogger = Substitute.For<ILogger>();
                
                macParser = new MacLineParser(mockDownloader, mockLogger);
                macImporter = new MacImporter(mockRepository, macParser, mockLogger, mockDownloader);
            });
        }

        [Test]
        public async Task ImportMacs_WillNotReplaceExistingMacs()
        {
            // Arrange
            var shorterEarlyMac = MacEntityBuilder.New.With(m => m.RowKey, "AA").Build();
            var shorterLateMac = MacEntityBuilder.New.With(m => m.RowKey, "ZZ").Build();
            var lastMac = MacEntityBuilder.New.With(m => m.RowKey, "ZZZ").Build();
            var oldMacs = new List<MultipleAlleleCodeEntity>
            {
                shorterEarlyMac,
                shorterLateMac,
                lastMac
            };
            var lastOldMac = lastMac.RowKey;
            const int numberOfNewMacs = 50;
            var newMacs = Enumerable.Range(0, numberOfNewMacs).Select(i => MacEntityBuilder.New.Build()).ToList();
            mockDownloader.DownloadAndUnzipStream().Returns(MacSourceFileBuilder.BuildMacFile(oldMacs.Concat(newMacs)));
            mockRepository.GetLastMacEntry().Returns(lastOldMac);
            
            // Act
            await macImporter.ImportLatestMultipleAlleleCodes();
            
            // Assert
            await mockRepository.Received().InsertMacs(Arg.Is<List<MultipleAlleleCodeEntity>>(x => x.Count == numberOfNewMacs));
            
        }
    }
}