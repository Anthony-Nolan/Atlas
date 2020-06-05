using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Models;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Repositories;
using Atlas.MultipleAlleleCodeDictionary.MacImportServices;
using Atlas.MultipleAlleleCodeDictionary.MacImportServices.SourceData;
using Atlas.MultipleAlleleCodeDictionary.Test.TestHelpers.Builders;
using Atlas.MultipleAlleleCodeDictionary.utils;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MultipleAlleleCodeDictionary.Test.UnitTests
{
    [TestFixture]
    internal class Tests
    {
        private IMacImporter macImporter;
        private IMacCodeDownloader mockDownloader;
        private IMacParser macParser;
        private IMacRepository mockRepository;

        [SetUp]
        public void Setup()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                macImporter = DependencyInjection.DependencyInjection.Provider.GetService<IMacImporter>();
                mockDownloader = DependencyInjection.DependencyInjection.Provider.GetService<IMacCodeDownloader>();
                mockRepository = DependencyInjection.DependencyInjection.Provider.GetService<IMacRepository>();
            });
        }

        [Test]
        public void TokenTest()
        {
            Assert.Pass();
        }
        [Test]
        public void ImportMacs_WillNotReplaceExistingMacs()
        {
            const int numberOfOldMacs = 50;
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
            macImporter.ImportLatestMultipleAlleleCodes();
            mockRepository.Received().InsertMacs(Arg.Is<List<MultipleAlleleCodeEntity>>(x => x.Count == 50));
            
        }
    }
}