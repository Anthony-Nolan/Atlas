using System;
using System.Threading.Tasks;
using Atlas.Common.Caching;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NUnit.Framework;

namespace Atlas.Common.Test.Caching
{
    [TestFixture]
    public class DisabledDistributedCacheProviderTests
    {
        private Fixture fixture;
        private FakeLogger<DisabledDistributedCacheProvider> logger;

        [SetUp]
        public void SetUp()
        {
            fixture = new Fixture();
            logger = new FakeLogger<DisabledDistributedCacheProvider>();
        }

        [Test]
        public async Task GetAsync_AfterSetAsync_ReturnsNull()
        {
            var provider = NewProvider();
            var key = NewKey();
            await provider.SetAsync(key, fixture.Create<byte[]>(), NewTimeToLive());

            (await provider.GetAsync(key)).Should().BeNull();
        }

        /// <summary>True, as a cache that keeps nothing cannot serve a stale value.</summary>
        [Test]
        public async Task SetAsync_ReturnsTrue()
        {
            var stored = await NewProvider().SetAsync(NewKey(), fixture.Create<byte[]>(), NewTimeToLive());

            stored.Should().BeTrue();
        }

        [Test]
        public async Task RemoveAsync_ReturnsTrue()
        {
            var removed = await NewProvider().RemoveAsync(NewKey());

            removed.Should().BeTrue();
        }

        [Test]
        public void Constructor_LogsThatNoCacheIsConfigured()
        {
            NewProvider();

            logger.Collector.GetSnapshot().Should().ContainSingle().Which.Level.Should().Be(LogLevel.Information);
        }

        /// <summary>The same checks as the cache in Redis, so that a caller's mistake shows in an app with no Redis too.</summary>
        [Test]
        public async Task SetAsync_WithATimeToLiveThatIsNotPositive_Throws()
        {
            var set = () => NewProvider().SetAsync(NewKey(), fixture.Create<byte[]>(), TimeSpan.Zero);

            await set.Should().ThrowAsync<ArgumentOutOfRangeException>();
        }

        [Test]
        public async Task GetAsync_WithTheDefaultKey_Throws()
        {
            var get = () => NewProvider().GetAsync(default);

            await get.Should().ThrowAsync<ArgumentException>();
        }

        private DisabledDistributedCacheProvider NewProvider() => new(logger);

        private DistributedCacheKey NewKey() => new(fixture.Create<string>(), fixture.Create<string>());

        private TimeSpan NewTimeToLive() => TimeSpan.FromMinutes(fixture.Create<int>());
    }
}
