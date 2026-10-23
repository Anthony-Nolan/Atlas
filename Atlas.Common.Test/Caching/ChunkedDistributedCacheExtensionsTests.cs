using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Common.Caching;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using AutoFixture;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.Common.Test.Caching;

[TestFixture]
public class ChunkedDistributedCacheExtensionsTests
{
    private const int ChunkSize = ChunkedDistributedCacheExtensions.ChunkSizeInBytes;

    private Fixture fixture;

    // The in-memory distributed cache, and a substitute that passes each call to it, so that a test can see the writes.
    private IDistributedCacheProvider store;
    private IDistributedCacheProvider cache;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        store = DistributedCacheBuilder.NewProvider();
        cache = Substitute.For<IDistributedCacheProvider>();
        cache.GetAsync(default, default).ReturnsForAnyArgs(call => store.GetAsync(call.Arg<DistributedCacheKey>(), call.Arg<CancellationToken>()));
        cache.SetAsync(default, default, default, default).ReturnsForAnyArgs(call =>
            store.SetAsync(call.Arg<DistributedCacheKey>(), call.Arg<byte[]>(), call.Arg<TimeSpan>(), call.Arg<CancellationToken>()));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(ChunkSize)]
    [TestCase(ChunkSize + 1)]
    [TestCase(3 * ChunkSize + 7)]
    public async Task GetChunkedAsync_AfterSetChunkedAsync_ReturnsTheValue(int length)
    {
        var key = NewKey();
        var value = NewValue(length);

        var stored = await cache.SetChunkedAsync(key, value, NewTimeToLive());
        var read = await cache.GetChunkedAsync(key);

        stored.Should().BeTrue();
        read.Should().Equal(value);
    }

    [TestCase(0, 0)]
    [TestCase(1, 1)]
    [TestCase(ChunkSize, 1)]
    [TestCase(ChunkSize + 1, 2)]
    public async Task SetChunkedAsync_StoresOneChunkForEachChunkSizeOrPartOfIt(int length, int expectedChunkCount)
    {
        var key = NewKey();

        await cache.SetChunkedAsync(key, NewValue(length), NewTimeToLive());

        ChunkWrites(key).Should().HaveCount(expectedChunkCount);
        ChunkedDistributedCacheExtensions.ChunkCount(length).Should().Be(expectedChunkCount);
    }

    [Test]
    public async Task SetChunkedAsync_StoresNoChunkLargerThanTheChunkSize()
    {
        var key = NewKey();

        await cache.SetChunkedAsync(key, NewValue(3 * ChunkSize + 1), NewTimeToLive());

        ChunkWrites(key).Should().OnlyContain(write => write.Value.Length <= ChunkSize);
    }

    [Test]
    public async Task SetChunkedAsync_StoresTheChunksBeforeTheManifest()
    {
        var key = NewKey();

        await cache.SetChunkedAsync(key, NewValue(2 * ChunkSize + 1), NewTimeToLive());

        Writes().Select(write => write.Key).Should().ContainSingle(writtenKey => writtenKey == key).And.EndWith(key);
    }

    [Test]
    public async Task SetChunkedAsync_GivesTheChunksOneHourMoreTimeToLiveThanTheManifest()
    {
        var key = NewKey();
        var timeToLive = NewTimeToLive();

        await cache.SetChunkedAsync(key, NewValue(2 * ChunkSize + 1), timeToLive);

        Writes().Single(write => write.Key == key).TimeToLive.Should().Be(timeToLive);
        ChunkWrites(key).Should().OnlyContain(write => write.TimeToLive == timeToLive + TimeSpan.FromHours(1));
    }

    /// <summary>So that the metrics of the cache name of the key count only the reads of whole values.</summary>
    [Test]
    public async Task SetChunkedAsync_StoresTheChunksUnderTheCacheNameOfTheKeyWithChunksAfterIt()
    {
        var key = NewKey();

        await cache.SetChunkedAsync(key, NewValue(2 * ChunkSize + 1), NewTimeToLive());

        ChunkWrites(key).Should().OnlyContain(write => write.Key.CacheName == $"{key.CacheName}-chunks");
    }

    /// <summary>Processes that miss one value at the same time each write it: Redis must then keep one copy, not one for each writer.</summary>
    [Test]
    public async Task SetChunkedAsync_TheSameValueTwice_StoresItUnderTheSameChunkKeys()
    {
        var key = NewKey();
        var value = NewValue(2 * ChunkSize + 1);
        await cache.SetChunkedAsync(key, value, NewTimeToLive());
        var chunkKeysOfTheFirstWrite = ChunkWrites(key).Select(write => write.Key).ToList();
        cache.ClearReceivedCalls();

        await cache.SetChunkedAsync(key, value.ToArray(), NewTimeToLive());

        ChunkWrites(key).Select(write => write.Key).Should().Equal(chunkKeysOfTheFirstWrite);
    }

    [Test]
    public async Task SetChunkedAsync_ADifferentValueUnderTheSameKey_StoresItUnderOtherChunkKeys()
    {
        var key = NewKey();
        await cache.SetChunkedAsync(key, NewValue(2 * ChunkSize + 1), NewTimeToLive());
        var chunkKeysOfTheFirstValue = ChunkWrites(key).Select(write => write.Key).ToList();
        cache.ClearReceivedCalls();

        await cache.SetChunkedAsync(key, NewValue(2 * ChunkSize + 1), NewTimeToLive());

        ChunkWrites(key).Select(write => write.Key).Should().NotIntersectWith(chunkKeysOfTheFirstValue);
    }

    [Test]
    public async Task SetChunkedAsync_WhenAChunkIsNotStored_ReturnsFalse_AndStoresNothingAfterIt()
    {
        var key = NewKey();
        var unreliableCache = Substitute.For<IDistributedCacheProvider>();
        unreliableCache.SetAsync(default, default, default, default).ReturnsForAnyArgs(true, false, true);

        var stored = await unreliableCache.SetChunkedAsync(key, NewValue(3 * ChunkSize), NewTimeToLive());

        stored.Should().BeFalse();
        await unreliableCache.ReceivedWithAnyArgs(2).SetAsync(default, default, default, default);
        await unreliableCache.DidNotReceive().SetAsync(key, Arg.Any<byte[]>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SetChunkedAsync_WithTheDefaultKey_ThrowsBeforeItStoresAChunk()
    {
        var set = () => cache.SetChunkedAsync(default, NewValue(ChunkSize), NewTimeToLive());

        await set.Should().ThrowAsync<ArgumentException>();
        await cache.DidNotReceiveWithAnyArgs().SetAsync(default, default, default, default);
    }

    [Test]
    public async Task GetChunkedAsync_WhenNothingIsStored_ReturnsNull()
    {
        (await cache.GetChunkedAsync(NewKey())).Should().BeNull();
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public async Task GetChunkedAsync_WhenAChunkIsMissing_ReturnsNull(int missingChunk)
    {
        var key = NewKey();
        await cache.SetChunkedAsync(key, NewValue(2 * ChunkSize + 1), NewTimeToLive());

        await store.RemoveAsync(ChunkWrites(key)[missingChunk].Key);

        (await cache.GetChunkedAsync(key)).Should().BeNull();
    }

    [Test]
    public async Task GetChunkedAsync_WhenAChunkHasAnotherLength_ReturnsNull()
    {
        var key = NewKey();
        await cache.SetChunkedAsync(key, NewValue(2 * ChunkSize + 1), NewTimeToLive());

        await store.SetAsync(ChunkWrites(key)[0].Key, NewValue(ChunkSize - 1), NewTimeToLive());

        (await cache.GetChunkedAsync(key)).Should().BeNull();
    }

    [Test]
    public async Task GetChunkedAsync_WhenTheValueUnderTheKeyIsNotAManifest_ReturnsNull()
    {
        var key = NewKey();
        await cache.SetChunkedAsync(key, NewValue(ChunkSize), NewTimeToLive());

        await store.SetAsync(key, fixture.Create<byte[]>(), NewTimeToLive());

        (await cache.GetChunkedAsync(key)).Should().BeNull();
    }

    /// <summary>
    /// Writer A stores its chunks, and stops before its manifest. Writer B then stores a different value, all of it. Then A
    /// stores its manifest. Each read returns a complete value: the value of the last manifest.
    /// </summary>
    [Test]
    public async Task GetChunkedAsync_WhenTwoWritersOfOneKeyInterleave_ReturnsTheCompleteValueOfTheLastManifest()
    {
        var key = NewKey();
        var valueOfA = NewValue(2 * ChunkSize + 1);
        var valueOfB = NewValue(2 * ChunkSize + 1);
        var manifestOfACanBeStored = new TaskCompletionSource();
        var writerA = Substitute.For<IDistributedCacheProvider>();
        writerA.SetAsync(default, default, default, default).ReturnsForAnyArgs(async call =>
        {
            if (call.Arg<DistributedCacheKey>() == key)
            {
                await manifestOfACanBeStored.Task;
            }

            return await store.SetAsync(call.Arg<DistributedCacheKey>(), call.Arg<byte[]>(), call.Arg<TimeSpan>(), call.Arg<CancellationToken>());
        });

        var writeOfA = writerA.SetChunkedAsync(key, valueOfA, NewTimeToLive());
        await store.SetChunkedAsync(key, valueOfB, NewTimeToLive());
        var readAfterB = await store.GetChunkedAsync(key);
        manifestOfACanBeStored.SetResult();
        await writeOfA;
        var readAfterA = await store.GetChunkedAsync(key);

        readAfterB.Should().Equal(valueOfB);
        readAfterA.Should().Equal(valueOfA);
    }

    private DistributedCacheKey NewKey() => new(fixture.Create<string>(), fixture.Create<string>());

    private TimeSpan NewTimeToLive() => TimeSpan.FromMinutes(fixture.Create<int>());

    private byte[] NewValue(int length)
    {
        var value = new byte[length];
        new Random(fixture.Create<int>()).NextBytes(value);
        return value;
    }

    /// <summary>The writes to <see cref="cache"/>, in their order.</summary>
    private List<(DistributedCacheKey Key, byte[] Value, TimeSpan TimeToLive)> Writes() =>
        cache.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(IDistributedCacheProvider.SetAsync))
            .Select(call => call.GetArguments())
            .Select(arguments => ((DistributedCacheKey)arguments[0], (byte[])arguments[1], (TimeSpan)arguments[2]))
            .ToList();

    /// <summary>The writes to <see cref="cache"/> that are not the manifest under <paramref name="key"/>: the chunks, in their order.</summary>
    private List<(DistributedCacheKey Key, byte[] Value, TimeSpan TimeToLive)> ChunkWrites(DistributedCacheKey key) =>
        Writes().Where(write => write.Key != key).ToList();
}
