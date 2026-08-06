using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Repositories;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;
using Atlas.MultipleAlleleCodeDictionary.Services;
using Atlas.MultipleAlleleCodeDictionary.Test.TestHelpers.Builders;
using AutoFixture;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MultipleAlleleCodeDictionary.Test.UnitTests
{
    [TestFixture]
    internal class MacCacheServiceTests
    {
        private Fixture fixture;
        private IMacRepository mockRepository;
        private IMacExpander mockExpander;
        private IMacStore macStore;
        private IAtlasLogger mockLogger;
        private IMacCacheService macCacheService;

        [SetUp]
        public void SetUp()
        {
            fixture = new Fixture();
            mockRepository = Substitute.For<IMacRepository>();
            mockExpander = Substitute.For<IMacExpander>();
            mockLogger = Substitute.For<IAtlasLogger>();
            // The store is a dependency free data structure, so the real one is used - these tests care about what ends up in it.
            macStore = new MacStore();
            macCacheService = new MacCacheService(mockLogger, macStore, mockRepository, mockExpander);
        }

        [Test]
        public async Task GetMacCode_WhenMacIsInStore_DoesNotQueryRepository()
        {
            var mac = fixture.Create<Mac>();
            macStore.AddMac(mac.Code, new MacValue(mac.Hla, mac.IsGeneric));

            var fetchedMac = await macCacheService.GetMacCode(mac.Code);

            await mockRepository.DidNotReceiveWithAnyArgs().GetMac(default);
            fetchedMac.Should().BeEquivalentTo(mac);
        }

        [Test]
        public async Task GetMacCode_WhenMacIsNotInStore_FetchesMacFromRepository()
        {
            var mac = fixture.Create<Mac>();
            mockRepository.GetMac(mac.Code).Returns(mac);

            var fetchedMac = await macCacheService.GetMacCode(mac.Code);

            fetchedMac.Should().BeSameAs(mac);
        }

        [Test]
        public async Task GetMacCode_WhenMacIsNotInStore_AddsFetchedMacToStore()
        {
            var mac = fixture.Create<Mac>();
            mockRepository.GetMac(mac.Code).Returns(mac);

            await macCacheService.GetMacCode(mac.Code);
            await macCacheService.GetMacCode(mac.Code);

            await mockRepository.Received(1).GetMac(mac.Code);
            macStore.TryGetMac(mac.Code, out var storedMac).Should().BeTrue();
            storedMac.Should().Be(new MacValue(mac.Hla, mac.IsGeneric));
        }

        [Test]
        public async Task GetHlaFromMac_ExpandsMacHeldInStore()
        {
            var mac = fixture.Create<Mac>();
            var firstField = fixture.Create<string>();
            var expandedHla = fixture.CreateMany<string>(3).ToList();
            macStore.AddMac(mac.Code, new MacValue(mac.Hla, mac.IsGeneric));
            mockExpander.ExpandMac(Arg.Is<Mac>(m => m.Hla == mac.Hla && m.IsGeneric == mac.IsGeneric), firstField).Returns(expandedHla);

            var hla = await macCacheService.GetHlaFromMac(mac.Code, firstField);

            hla.Should().BeEquivalentTo(expandedHla);
        }

        [Test]
        public async Task GetHlaFromMac_WhenMacIsUnrecognised_Throws()
        {
            mockRepository.GetMac(default).ReturnsForAnyArgs((Mac) null);

            var act = () => macCacheService.GetHlaFromMac(fixture.Create<string>(), fixture.Create<string>());

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Test]
        public async Task PreWarmAllMacs_AddsEveryStreamedMacToStore()
        {
            var macs = MacBuilder.New.CreateMany(10).ToList();
            mockRepository.StreamAllMacs().Returns(macs.ToAsyncEnumerable());

            await macCacheService.PreWarmAllMacs();

            macStore.Count.Should().Be(macs.Count);
            macs.Should().AllSatisfy(mac =>
            {
                macStore.TryGetMac(mac.Code, out var storedMac).Should().BeTrue();
                storedMac.Should().Be(new MacValue(mac.Hla, mac.IsGeneric));
            });
        }

        [Test]
        public async Task PreWarmAllMacs_DoesNotFetchWholeTableIntoAList()
        {
            mockRepository.StreamAllMacs().Returns(MacBuilder.New.CreateMany(10).ToList().ToAsyncEnumerable());

            await macCacheService.PreWarmAllMacs();

            await mockRepository.DidNotReceive().GetAllMacs();
        }

        /// <summary>
        /// The pre-warm's duration is uninterpretable without its size: a slow load because the table is large and a
        /// slow load per row imply different fixes. The count is also how the store's resident memory gets priced -
        /// it is a process-wide singleton with no expiry, so it outlives the refresh that filled it.
        /// </summary>
        [Test]
        public async Task PreWarmAllMacs_RecordsHowManyMacsWereLoaded()
        {
            var macs = MacBuilder.New.CreateMany(10).ToList();
            mockRepository.StreamAllMacs().Returns(macs.ToAsyncEnumerable());

            await macCacheService.PreWarmAllMacs();

            mockLogger.Received(1).SendMetric(
                DataRefreshMetrics.CountMetric,
                macs.Count,
                Arg.Is<Dictionary<string, string>>(d =>
                    d[DataRefreshMetrics.OperationDimension] == DataRefreshMetrics.Operation_MacsPreWarmed));
        }

        [Test]
        public async Task PreWarmAllMacs_WhenCalledRepeatedly_OnlyReadsMacsOnce()
        {
            mockRepository.StreamAllMacs().Returns(MacBuilder.New.CreateMany(10).ToList().ToAsyncEnumerable());

            await macCacheService.PreWarmAllMacs();
            await macCacheService.PreWarmAllMacs();

            mockRepository.Received(1).StreamAllMacs();
        }

        [Test]
        public async Task GetMacCode_WhenStoreHasBeenWarmed_DoesNotQueryRepository()
        {
            var macs = MacBuilder.New.CreateMany(10).ToList();
            mockRepository.StreamAllMacs().Returns(macs.ToAsyncEnumerable());
            await macCacheService.PreWarmAllMacs();

            var fetchedMacs = new List<Mac>();
            foreach (var mac in macs)
            {
                fetchedMacs.Add(await macCacheService.GetMacCode(mac.Code));
            }

            await mockRepository.DidNotReceiveWithAnyArgs().GetMac(default);
            fetchedMacs.Should().BeEquivalentTo(macs);
        }
    }
}
