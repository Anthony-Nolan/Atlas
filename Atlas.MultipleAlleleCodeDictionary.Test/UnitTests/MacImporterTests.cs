using System;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Repositories;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;
using Atlas.MultipleAlleleCodeDictionary.Services.MacImport;
using Atlas.MultipleAlleleCodeDictionary.Test.TestHelpers.Builders;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Notifications;
using NSubstitute.ExceptionExtensions;

namespace Atlas.MultipleAlleleCodeDictionary.Test.UnitTests
{
    [TestFixture]
    internal class MacImporterTests
    {
        private IMacImporter macImporter;
        private IMacFetcher mockFetcher;
        private IMacRepository mockRepository;
        private ILogger mockLogger;
        private INotificationSender mockNotificationSender;

        [SetUp]
        public void Setup()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                mockFetcher = Substitute.For<IMacFetcher>();
                mockRepository = Substitute.For<IMacRepository>();
                mockLogger = Substitute.For<ILogger>();
                mockNotificationSender = Substitute.For<INotificationSender>();
                macImporter = new MacImporter(mockRepository, mockFetcher, mockLogger, mockNotificationSender);
            });
        }

        [Test]
        public async Task ImportMacs_FetchesLatestMacs()
        {
            const string lastOldMac = "ZZZ";
            mockRepository.GetLastMacEntry().Returns(lastOldMac);

            await macImporter.ImportLatestMacs();

#pragma warning disable 4014
            // disabled warning as the method is async, but not awaitable

            mockFetcher.Received().FetchAndLazilyParseMacsSince(lastOldMac);

#pragma warning restore 4014
        }

        [Test]
        public async Task ImportMacs_OnlyStoresLatestMacs()
        {
            const string lastOldMac = "ZZZ";
            mockRepository.GetLastMacEntry().Returns(lastOldMac);

            const int numberOfNewMacs = 50;
            var newMacs = Enumerable.Range(0, numberOfNewMacs).Select(i => MacBuilder.New.Build()).ToList();
            mockFetcher.FetchAndLazilyParseMacsSince(default).ReturnsForAnyArgs(newMacs.ToAsyncEnumerable());

            await macImporter.ImportLatestMacs();

            await mockRepository.Received().InsertMacs(Arg.Is<IEnumerable<Mac>>(x => x.Count() == numberOfNewMacs));
        }

        [Test]
        public async Task ImportMacs_OnFailure_SendsAlert()
        {
            mockRepository.InsertMacs(default).ThrowsForAnyArgs(new Exception());

            try
            {
                await macImporter.ImportLatestMacs();
            }
            catch (Exception)
            {
                await mockNotificationSender.ReceivedWithAnyArgs().SendAlert(default, default, default, default);
            }
        }
    }
}