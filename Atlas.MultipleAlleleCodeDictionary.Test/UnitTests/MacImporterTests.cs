using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Repositories;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;
using Atlas.MultipleAlleleCodeDictionary.Services.MacImport;
using Atlas.MultipleAlleleCodeDictionary.Test.TestHelpers.Builders;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MultipleAlleleCodeDictionary.Test.UnitTests
{
    [TestFixture]
    internal class MacImporterTests
    {
        private IMacImporter macImporter;
        private IMacStreamer mockStreamer;
        private IMacRepository mockRepository;
        private ILogger mockLogger;

        [SetUp]
        public void Setup()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                mockStreamer = Substitute.For<IMacStreamer>();
                mockRepository = Substitute.For<IMacRepository>();
                mockLogger = Substitute.For<ILogger>();
                macImporter = new MacImporter(mockRepository, mockStreamer, mockLogger);
            });
        }

        [Test]
        public async Task ImportMacs_StreamsLatestMacs()
        {
            const string lastOldMac = "ZZZ";
            mockRepository.GetLastMacEntry().Returns(lastOldMac);

            await macImporter.ImportLatestMacs();

            await mockStreamer.Received().StreamMacsSince(lastOldMac);
        }

        [Test]
        public async Task ImportMacs_OnlyStoresLatestMacs()
        {
            const string lastOldMac = "ZZZ";
            mockRepository.GetLastMacEntry().Returns(lastOldMac);

            const int numberOfNewMacs = 50;
            var newMacs = Enumerable.Range(0, numberOfNewMacs).Select(i => MacBuilder.New.Build()).ToList();
            mockStreamer.StreamMacsSince(default).ReturnsForAnyArgs(newMacs.ToAsyncEnumerable());
            
            await macImporter.ImportLatestMacs();
            
            await mockRepository.Received().InsertMacs(Arg.Is<IEnumerable<Mac>>(x => x.Count() == numberOfNewMacs));
        }
    }
}