using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Repositories;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;
using Atlas.MultipleAlleleCodeDictionary.Services.MacImportServices;
using Atlas.MultipleAlleleCodeDictionary.Test.TestHelpers.Builders;
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
                
                macParser = new MacLineParser(mockLogger);
                macImporter = new MacImporter(mockRepository, macParser, mockLogger, mockDownloader);
            });
        }

        [Test]
        public async Task ImportMacs_WillNotReplaceExistingMacs()
        {
            var shorterEarlyMac = MacBuilder.New.With(m => m.Code, "AA").Build();
            var shorterLateMac = MacBuilder.New.With(m => m.Code, "ZZ").Build();
            var lastMac = MacBuilder.New.With(m => m.Code, "ZZZ").Build();
            var oldMacs = new List<Mac>
            {
                shorterEarlyMac,
                shorterLateMac,
                lastMac
            };
            var lastOldMac = lastMac.Code;
            const int numberOfNewMacs = 50;
            var newMacs = Enumerable.Range(0, numberOfNewMacs).Select(i => MacBuilder.New.Build()).ToList();
            mockDownloader.DownloadAndUnzipStream().Returns(MacSourceFileBuilder.BuildMacFile(oldMacs.Concat(newMacs)));
            mockRepository.GetLastMacEntry().Returns(lastOldMac);
            
            await macImporter.ImportLatestMacs();
            
            await mockRepository.Received().InsertMacs(Arg.Is<List<Mac>>(x => x.Count == numberOfNewMacs));
        }
    }
}