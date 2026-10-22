using System;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Common.Caching;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.Common.Test.Caching
{
    [TestFixture]
    public class DistributedCacheProviderTests
    {
        private Fixture fixture;
        private IDistributedCacheMetrics metrics;
        private FakeLogger<DistributedCacheProvider> logger;
        private FakeTimeProvider timeProvider;

        [SetUp]
        public void SetUp()
        {
            fixture = new Fixture();
            metrics = Substitute.For<IDistributedCacheMetrics>();
            logger = new FakeLogger<DistributedCacheProvider>();
            timeProvider = new FakeTimeProvider();
        }

        [Test]
        public async Task GetAsync_AfterSetAsync_ReturnsTheStoredValue()
        {
            var provider = ProviderOver(DistributedCacheBuilder.NewSharedStore());
            var key = NewKey();
            var value = fixture.Create<byte[]>();

            await provider.SetAsync(key, value, NewTimeToLive());

            (await provider.GetAsync(key)).Should().Equal(value);
        }

        [Test]
        public async Task SetAsync_WhenTheStoreTakesTheValue_ReturnsTrue()
        {
            var provider = ProviderOver(DistributedCacheBuilder.NewSharedStore());

            var stored = await provider.SetAsync(NewKey(), fixture.Create<byte[]>(), NewTimeToLive());

            stored.Should().BeTrue();
        }

        [Test]
        public async Task GetAsync_WhenNothingIsStored_ReturnsNull()
        {
            var provider = ProviderOver(DistributedCacheBuilder.NewSharedStore());

            (await provider.GetAsync(NewKey())).Should().BeNull();
        }

        [Test]
        public async Task RemoveAsync_AfterSetAsync_LeavesNoValue()
        {
            var provider = ProviderOver(DistributedCacheBuilder.NewSharedStore());
            var key = NewKey();
            await provider.SetAsync(key, fixture.Create<byte[]>(), NewTimeToLive());

            var removed = await provider.RemoveAsync(key);

            removed.Should().BeTrue();
            (await provider.GetAsync(key)).Should().BeNull();
        }

        /// <summary>Two providers over one store act as two processes that share one Redis.</summary>
        [Test]
        public async Task GetAsync_OfAnotherProviderOverTheSameStore_ReturnsTheStoredValue()
        {
            var store = DistributedCacheBuilder.NewSharedStore();
            var key = NewKey();
            var value = fixture.Create<byte[]>();
            await ProviderOver(store).SetAsync(key, value, NewTimeToLive());

            (await ProviderOver(store).GetAsync(key)).Should().Equal(value);
        }

        [Test]
        public async Task SetAsync_SendsTheTimeToLiveAsAnAbsoluteExpiryOnly()
        {
            var store = Substitute.For<IDistributedCache>();
            var key = NewKey();
            var value = fixture.Create<byte[]>();
            var timeToLive = NewTimeToLive();

            await ProviderOver(store).SetAsync(key, value, timeToLive);

            await store.Received(1).SetAsync(
                key.ToString(),
                value,
                Arg.Is<DistributedCacheEntryOptions>(options =>
                    options.AbsoluteExpirationRelativeToNow == timeToLive
                    && options.AbsoluteExpiration == null
                    && options.SlidingExpiration == null),
                Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task GetAsync_WhenTheStoreFails_ReturnsNull()
        {
            var provider = ProviderOver(FailingStore());

            (await provider.GetAsync(NewKey())).Should().BeNull();
        }

        [Test]
        public async Task GetAsync_WhenTheStoreFails_LogsAWarningWithTheCacheNameButNotTheId()
        {
            var key = NewKey();

            await ProviderOver(FailingStore()).GetAsync(key);

            var log = logger.Collector.GetSnapshot().Should().ContainSingle().Which;
            log.Level.Should().Be(LogLevel.Warning);
            log.Message.Should().Contain(key.CacheName).And.NotContain(key.Id);
        }

        [Test]
        public async Task SetAsync_WhenTheStoreFails_ReturnsFalse()
        {
            var provider = ProviderOver(FailingStore());

            var stored = await provider.SetAsync(NewKey(), fixture.Create<byte[]>(), NewTimeToLive());

            stored.Should().BeFalse();
        }

        [Test]
        public async Task RemoveAsync_WhenTheStoreFails_ReturnsFalse()
        {
            var provider = ProviderOver(FailingStore());

            var removed = await provider.RemoveAsync(NewKey());

            removed.Should().BeFalse();
        }

        [Test]
        public async Task GetAsync_WhenTheCallerCancels_Throws()
        {
            var provider = ProviderOver(DistributedCacheBuilder.NewSharedStore());
            using var cancellation = new CancellationTokenSource();
            await cancellation.CancelAsync();

            var get = () => provider.GetAsync(NewKey(), cancellation.Token);

            await get.Should().ThrowAsync<OperationCanceledException>();
        }

        [TestCase(0)]
        [TestCase(-1)]
        public async Task SetAsync_WithATimeToLiveThatIsNotPositive_Throws(int minutes)
        {
            var provider = ProviderOver(DistributedCacheBuilder.NewSharedStore());

            var set = () => provider.SetAsync(NewKey(), fixture.Create<byte[]>(), TimeSpan.FromMinutes(minutes));

            await set.Should().ThrowAsync<ArgumentOutOfRangeException>();
        }

        [Test]
        public async Task SetAsync_WithNoValue_Throws()
        {
            var provider = ProviderOver(DistributedCacheBuilder.NewSharedStore());

            var set = () => provider.SetAsync(NewKey(), null, NewTimeToLive());

            await set.Should().ThrowAsync<ArgumentNullException>();
        }

        [Test]
        public async Task GetAsync_WithTheDefaultKey_Throws()
        {
            var provider = ProviderOver(DistributedCacheBuilder.NewSharedStore());

            var get = () => provider.GetAsync(default);

            await get.Should().ThrowAsync<ArgumentException>();
        }

        [Test]
        public async Task GetAsync_AfterRepeatedFaults_DoesNotCallTheStore()
        {
            var store = FailingStore();
            var provider = ProviderOver(store);
            await OpenTheCircuit(provider);
            store.ClearReceivedCalls();

            var value = await provider.GetAsync(NewKey());

            value.Should().BeNull();
            await store.DidNotReceiveWithAnyArgs().GetAsync(default, default);
        }

        [Test]
        public async Task GetAsync_WhenTheCircuitIsOpen_RecordsTheCallAsSkipped()
        {
            var provider = ProviderOver(FailingStore());
            await OpenTheCircuit(provider);
            var key = NewKey();

            await provider.GetAsync(key);

            metrics.Received(1).RecordOperation(key.CacheName, DistributedCacheOperation.Get, DistributedCacheResult.Skipped, Arg.Any<TimeSpan>());
        }

        [Test]
        public async Task SetAsync_WhenTheCircuitIsOpen_ReturnsFalse()
        {
            var provider = ProviderOver(FailingStore());
            await OpenTheCircuit(provider);

            var stored = await provider.SetAsync(NewKey(), fixture.Create<byte[]>(), NewTimeToLive());

            stored.Should().BeFalse();
        }

        [Test]
        public async Task GetAsync_AfterTheBreakDuration_CallsTheStoreAgain()
        {
            var store = FailingStore();
            var provider = ProviderOver(store);
            await OpenTheCircuit(provider);
            timeProvider.Advance(DistributedCacheProvider.BreakerBreakDuration);
            store.ClearReceivedCalls();

            await provider.GetAsync(NewKey());

            await store.ReceivedWithAnyArgs(1).GetAsync(default, default);
        }

        [Test]
        public async Task GetAsync_OnAHit_RecordsAHitAndTheValueSize()
        {
            var provider = ProviderOver(DistributedCacheBuilder.NewSharedStore());
            var key = NewKey();
            var value = fixture.Create<byte[]>();
            await provider.SetAsync(key, value, NewTimeToLive());
            metrics.ClearReceivedCalls();

            await provider.GetAsync(key);

            metrics.Received(1).RecordOperation(key.CacheName, DistributedCacheOperation.Get, DistributedCacheResult.Hit, Arg.Any<TimeSpan>());
            metrics.Received(1).RecordValueSize(key.CacheName, DistributedCacheOperation.Get, value.Length);
        }

        [Test]
        public async Task GetAsync_OnAMiss_RecordsAMiss()
        {
            var key = NewKey();

            await ProviderOver(DistributedCacheBuilder.NewSharedStore()).GetAsync(key);

            metrics.Received(1).RecordOperation(key.CacheName, DistributedCacheOperation.Get, DistributedCacheResult.Miss, Arg.Any<TimeSpan>());
            metrics.DidNotReceiveWithAnyArgs().RecordValueSize(default, default, default);
        }

        [Test]
        public async Task GetAsync_WhenTheStoreFails_RecordsAFailure()
        {
            var key = NewKey();

            await ProviderOver(FailingStore()).GetAsync(key);

            metrics.Received(1).RecordOperation(key.CacheName, DistributedCacheOperation.Get, DistributedCacheResult.Failed, Arg.Any<TimeSpan>());
        }

        [Test]
        public async Task SetAsync_RecordsASuccessAndTheValueSize()
        {
            var key = NewKey();
            var value = fixture.Create<byte[]>();

            await ProviderOver(DistributedCacheBuilder.NewSharedStore()).SetAsync(key, value, NewTimeToLive());

            metrics.Received(1).RecordOperation(key.CacheName, DistributedCacheOperation.Set, DistributedCacheResult.Succeeded, Arg.Any<TimeSpan>());
            metrics.Received(1).RecordValueSize(key.CacheName, DistributedCacheOperation.Set, value.Length);
        }

        [Test]
        public async Task RemoveAsync_RecordsASuccess()
        {
            var key = NewKey();

            await ProviderOver(DistributedCacheBuilder.NewSharedStore()).RemoveAsync(key);

            metrics.Received(1).RecordOperation(key.CacheName, DistributedCacheOperation.Remove, DistributedCacheResult.Succeeded, Arg.Any<TimeSpan>());
        }

        [Test]
        public async Task GetAsync_RecordsHowLongTheStoreTook()
        {
            var store = Substitute.For<IDistributedCache>();
            var duration = TimeSpan.FromMilliseconds(fixture.Create<int>());
            store.GetAsync(default, default).ReturnsForAnyArgs(_ =>
            {
                timeProvider.Advance(duration);
                return Task.FromResult<byte[]>(null);
            });
            var key = NewKey();

            await ProviderOver(store).GetAsync(key);

            metrics.Received(1).RecordOperation(key.CacheName, DistributedCacheOperation.Get, DistributedCacheResult.Miss, duration);
        }

        [Test]
        public void Dispose_DisposesTheStore()
        {
            var store = Substitute.For<IDistributedCache, IDisposable>();

            ProviderOver(store).Dispose();

            ((IDisposable)store).Received(1).Dispose();
        }

        private DistributedCacheProvider ProviderOver(IDistributedCache store) => new(store, metrics, logger, timeProvider);

        private DistributedCacheKey NewKey() => new(fixture.Create<string>(), fixture.Create<string>());

        private TimeSpan NewTimeToLive() => TimeSpan.FromMinutes(fixture.Create<int>());

        private IDistributedCache FailingStore()
        {
            var store = Substitute.For<IDistributedCache>();
            store.GetAsync(default, default).ThrowsAsyncForAnyArgs(fixture.Create<Exception>());
            store.SetAsync(default, default, default, default).ThrowsAsyncForAnyArgs(fixture.Create<Exception>());
            store.RemoveAsync(default, default).ThrowsAsyncForAnyArgs(fixture.Create<Exception>());
            return store;
        }

        private async Task OpenTheCircuit(IDistributedCacheProvider provider)
        {
            for (var call = 0; call < DistributedCacheProvider.BreakerMinimumThroughput; call++)
            {
                await provider.GetAsync(NewKey());
            }
        }
    }
}
