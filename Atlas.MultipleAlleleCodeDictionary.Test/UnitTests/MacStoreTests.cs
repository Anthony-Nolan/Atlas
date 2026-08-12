using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Atlas.MultipleAlleleCodeDictionary.Services;
using AutoFixture;
using AwesomeAssertions;
using NUnit.Framework;

namespace Atlas.MultipleAlleleCodeDictionary.Test.UnitTests
{
    [TestFixture]
    internal class MacStoreTests
    {
        private Fixture fixture;
        private IMacStore macStore;

        [SetUp]
        public void SetUp()
        {
            fixture = new Fixture();
            macStore = new MacStore();
        }

        [Test]
        public void TryGetMac_WhenMacAdded_ReturnsAddedMac()
        {
            var code = fixture.Create<string>();
            var mac = fixture.Create<MacValue>();

            macStore.AddMac(code, mac);

            macStore.TryGetMac(code, out var fetchedMac).Should().BeTrue();
            fetchedMac.Should().Be(mac);
        }

        [Test]
        public void TryGetMac_WhenMacNotAdded_ReturnsFalse()
        {
            macStore.AddMac(fixture.Create<string>(), fixture.Create<MacValue>());

            macStore.TryGetMac(fixture.Create<string>(), out _).Should().BeFalse();
        }

        [Test]
        public void AddMac_WhenMacAlreadyPresent_OverwritesIt()
        {
            var code = fixture.Create<string>();
            var latestMac = fixture.Create<MacValue>();

            macStore.AddMac(code, fixture.Create<MacValue>());
            macStore.AddMac(code, latestMac);

            macStore.TryGetMac(code, out var fetchedMac);
            fetchedMac.Should().Be(latestMac);
            macStore.Count.Should().Be(1);
        }

        [Test]
        public async Task WarmOnce_RunsWarmAction()
        {
            var warmCount = 0;

            await macStore.WarmOnce(() =>
            {
                warmCount++;
                return Task.CompletedTask;
            });

            warmCount.Should().Be(1);
        }

        [Test]
        public async Task WarmOnce_WhenCalledRepeatedly_RunsWarmActionOnlyOnce()
        {
            var warmCount = 0;

            Task Warm()
            {
                warmCount++;
                return Task.CompletedTask;
            }

            await macStore.WarmOnce(Warm);
            await macStore.WarmOnce(Warm);
            await macStore.WarmOnce(Warm);

            warmCount.Should().Be(1);
        }

        [Test]
        public async Task WarmOnce_WhenCalledConcurrently_RunsWarmActionOnlyOnce()
        {
            var warmCount = 0;

            async Task Warm()
            {
                Interlocked.Increment(ref warmCount);
                await Task.Delay(20);
            }

            await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => macStore.WarmOnce(Warm))));

            warmCount.Should().Be(1);
        }

        [Test]
        public async Task WarmOnce_WhenWarmActionThrows_AllowsARetry()
        {
            var warmCount = 0;

            Task Warm()
            {
                warmCount++;
                throw new Exception(fixture.Create<string>());
            }

            var act = () => macStore.WarmOnce(Warm);

            await act.Should().ThrowAsync<Exception>();
            await act.Should().ThrowAsync<Exception>();

            warmCount.Should().Be(2);
        }
    }
}
