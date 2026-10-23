using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Test.TestHelpers;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using HfSetHaplotypeNames = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.LociInfo<string>;

namespace Atlas.MatchPrediction.Test.Services.HaplotypeFrequencies;

[TestFixture]
internal class HaplotypeFrequencyCacheTests
{
    private const int ActiveSetCacheExpiryMinutes = 5;
    private static readonly TimeSpan ActiveSetLocalEntryLifetime = TimeSpan.FromMinutes(ActiveSetCacheExpiryMinutes);

    private const int SetCacheExpiryMinutes = 60;
    private static readonly TimeSpan SetLocalEntryLifetime = TimeSpan.FromMinutes(SetCacheExpiryMinutes);

    /// <summary>How often a set in use is checked against its row: the expiry of the active sets.</summary>
    private static readonly TimeSpan SetCheckInterval = ActiveSetLocalEntryLifetime;

    /// <summary>The estimated memory of a set of one row: the byte limit of the eviction tests is a number of these.</summary>
    private const long OneRowSetSize = FrequencySetCacheEntry.EstimatedBytesPerHaplotype;

    /// <summary>Much longer than a test takes in real time - see <see cref="MoveClockTo"/>.</summary>
    private static readonly TimeSpan ClockMargin = TimeSpan.FromSeconds(5);

    private IHaplotypeFrequenciesRepository frequencyRepository;
    private IHaplotypeFrequencySetRepository frequencySetRepository;
    private IFrequencyConsolidator frequencyConsolidator;
    private IMatchPredictionLogger<MatchProbabilityLoggingContext> logger;

    // The clock of the local caches, so that a test can move past the expiry of a local entry.
    private DateTimeOffset now;
    private ISystemClock clock;

    private Fixture fixture;

    private HaplotypeFrequencyCache sut;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        frequencyRepository = Substitute.For<IHaplotypeFrequenciesRepository>();
        frequencySetRepository = Substitute.For<IHaplotypeFrequencySetRepository>();
        frequencyConsolidator = Substitute.For<IFrequencyConsolidator>();
        logger = Substitute.For<IMatchPredictionLogger<MatchProbabilityLoggingContext>>();

        now = DateTimeOffset.UtcNow;
        clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(_ => now);

        // Pinned rather than left to AutoFixture: AwaitConsolidatedFrequencyWarm decides whether the pre-consolidation
        // is on the critical path, so every test below would otherwise be exercising whichever mode the fixture picked.
        sut = BuildSut(awaitConsolidatedFrequencyWarm: false);
    }

    private HaplotypeFrequencyCache BuildSut(bool awaitConsolidatedFrequencyWarm, long cacheCapacityInBytes = long.MaxValue) =>
        BuildSutWithCacheProvider(awaitConsolidatedFrequencyWarm, cacheCapacityInBytes).Sut;

    /// <summary>The cache of one process: a local cache of its own, over <paramref name="distributedCache"/>.</summary>
    private HaplotypeFrequencyCache BuildInstance(IDistributedCacheProvider distributedCache) =>
        BuildSutWithCacheProvider(distributedCache: distributedCache).Sut;

    /// <summary>
    /// Sets the clock of the local caches to <paramref name="afterTheRealTime"/> after the real time. The clock follows the
    /// real time because LazyCache sets the expiry of an entry that <c>GetOrAddAsync</c> stores from the real clock, and
    /// the local cache compares it with this clock.
    /// </summary>
    private void MoveClockTo(TimeSpan afterTheRealTime) => now = DateTimeOffset.UtcNow + afterTheRealTime;

    private (HaplotypeFrequencyCache Sut, IHaplotypeFrequencySetCacheProvider CacheProvider) BuildSutWithCacheProvider(
        bool awaitConsolidatedFrequencyWarm = false,
        long cacheCapacityInBytes = long.MaxValue,
        IDistributedCacheProvider distributedCache = null)
    {
        var cacheSettings = fixture.Build<HaplotypeFrequencySetCacheSettings>()
            .With(x => x.ActiveSetCacheExpiryMinutes, ActiveSetCacheExpiryMinutes)
            .With(x => x.SetCacheExpiryMinutes, SetCacheExpiryMinutes)
            .With(x => x.AwaitConsolidatedFrequencyWarm, awaitConsolidatedFrequencyWarm)
            .Create();

        var cacheProvider = new HaplotypeFrequencySetCacheProvider(
            AppCacheBuilder.NewCacheOver(new MemoryCache(new MemoryCacheOptions { Clock = clock })));
        var residencyTracker = new FrequencySetResidencyTracker(
            cacheCapacityInBytes,
            evictedSetId => cacheProvider.Cache.Remove(HaplotypeFrequencyCache.AllFrequenciesCacheKey(evictedSetId)),
            NullLogger<FrequencySetResidencyTracker>.Instance
        );

        var sut = new HaplotypeFrequencyCache(
            cacheProvider,
            distributedCache ?? DistributedCacheBuilder.NewProvider(),
            residencyTracker,
            frequencyRepository,
            frequencySetRepository,
            frequencyConsolidator,
            logger,
            Options.Create(cacheSettings)
        );

        return (sut, cacheProvider);
    }

    [Test]
    public async Task GetActiveHaplotypeFrequencySets_MultipleCalls_QueriesRepositoryOnce()
    {
        var activeSet = fixture.Create<Data.Models.HaplotypeFrequencySet>();
        frequencySetRepository.GetAllActiveSets().Returns([activeSet]);

        await sut.GetActiveHaplotypeFrequencySets();
        await sut.GetActiveHaplotypeFrequencySets();

        await frequencySetRepository.Received(1).GetAllActiveSets();
    }

    [Test]
    public async Task RefreshActiveHaplotypeFrequencySets_ThisInstanceReturnsTheNewSetsAtOnce()
    {
        var firstActiveSet = fixture.Create<Data.Models.HaplotypeFrequencySet>();
        var secondActiveSet = fixture.Create<Data.Models.HaplotypeFrequencySet>();
        frequencySetRepository.GetAllActiveSets().Returns([firstActiveSet], [secondActiveSet]);

        await sut.GetActiveHaplotypeFrequencySets();
        await sut.RefreshActiveHaplotypeFrequencySets();
        var result = await sut.GetActiveHaplotypeFrequencySets();

        result.Values.Should().ContainSingle().Which.Id.Should().Be(secondActiveSet.Id);
        await frequencySetRepository.Received(2).GetAllActiveSets();
    }

    [Test]
    public async Task GetActiveHaplotypeFrequencySets_WhenAnotherInstanceStoredTheSets_DoesNotQueryTheRepository()
    {
        var store = DistributedCacheBuilder.NewSharedStore();
        var instanceA = BuildInstance(DistributedCacheBuilder.NewProviderOver(store));
        var instanceB = BuildInstance(DistributedCacheBuilder.NewProviderOver(store));
        var globalSet = fixture.Build<Data.Models.HaplotypeFrequencySet>()
            .With(x => x.RegistryCode, (string)null)
            .With(x => x.EthnicityCode, (string)null)
            .Create();
        frequencySetRepository.GetAllActiveSets().Returns([globalSet, fixture.Create<Data.Models.HaplotypeFrequencySet>()]);

        var setsOfA = await instanceA.GetActiveHaplotypeFrequencySets();
        var setsOfB = await instanceB.GetActiveHaplotypeFrequencySets();

        setsOfB.Should().BeEquivalentTo(setsOfA);
        setsOfB.Should().ContainKey((null, null));
        await frequencySetRepository.Received(1).GetAllActiveSets();
    }

    [Test]
    public async Task GetActiveHaplotypeFrequencySets_AfterAnotherInstanceRefreshes_ReturnsTheNewSetsWhenTheLocalEntryExpires()
    {
        var store = DistributedCacheBuilder.NewSharedStore();
        var instanceA = BuildInstance(DistributedCacheBuilder.NewProviderOver(store));
        var instanceB = BuildInstance(DistributedCacheBuilder.NewProviderOver(store));
        var replacedSet = fixture.Create<Data.Models.HaplotypeFrequencySet>();
        var newSet = fixture.Create<Data.Models.HaplotypeFrequencySet>();
        frequencySetRepository.GetAllActiveSets().Returns([replacedSet], [newSet]);

        await instanceB.GetActiveHaplotypeFrequencySets();
        await instanceA.RefreshActiveHaplotypeFrequencySets();
        var setsOfBBeforeTheExpiry = await instanceB.GetActiveHaplotypeFrequencySets();
        MoveClockTo(ActiveSetLocalEntryLifetime + ClockMargin);
        var setsOfBAfterTheExpiry = await instanceB.GetActiveHaplotypeFrequencySets();

        setsOfBBeforeTheExpiry.Values.Should().ContainSingle().Which.Id.Should().Be(replacedSet.Id);
        setsOfBAfterTheExpiry.Values.Should().ContainSingle().Which.Id.Should().Be(newSet.Id);
        // Once for the first read of B, and once for the refresh of A: the read of B after the expiry used the
        // distributed cache.
        await frequencySetRepository.Received(2).GetAllActiveSets();
    }

    [Test]
    public async Task GetActiveHaplotypeFrequencySets_LocalEntryExpiresAFixedTimeAfterItWasStored()
    {
        var instance = BuildInstance(UnreachableDistributedCache());
        frequencySetRepository.GetAllActiveSets().Returns([fixture.Create<Data.Models.HaplotypeFrequencySet>()]);

        await instance.GetActiveHaplotypeFrequencySets();
        MoveClockTo(ActiveSetLocalEntryLifetime - ClockMargin);
        // A sliding expiry would start again from this read.
        await instance.GetActiveHaplotypeFrequencySets();
        MoveClockTo(ActiveSetLocalEntryLifetime + ClockMargin);
        await instance.GetActiveHaplotypeFrequencySets();

        await frequencySetRepository.Received(2).GetAllActiveSets();
    }

    [Test]
    public async Task GetActiveHaplotypeFrequencySets_ConcurrentCallsWhenTheDistributedCacheMisses_ReadTheDatabaseOnce()
    {
        var instance = BuildInstance(UnreachableDistributedCache());
        var databaseRead = new TaskCompletionSource<IReadOnlyCollection<Data.Models.HaplotypeFrequencySet>>();
        frequencySetRepository.GetAllActiveSets().Returns(databaseRead.Task);

        var calls = Enumerable.Range(0, 10).Select(_ => instance.GetActiveHaplotypeFrequencySets()).ToList();
        databaseRead.SetResult([fixture.Create<Data.Models.HaplotypeFrequencySet>()]);
        await Task.WhenAll(calls);

        await frequencySetRepository.Received(1).GetAllActiveSets();
    }

    [Test]
    public async Task GetActiveHaplotypeFrequencySets_OnAMiss_StoresTheSetsForTwentyFourHours()
    {
        var distributedCache = UnreachableDistributedCache();
        var instance = BuildInstance(distributedCache);
        frequencySetRepository.GetAllActiveSets().Returns([fixture.Create<Data.Models.HaplotypeFrequencySet>()]);

        await instance.GetActiveHaplotypeFrequencySets();

        await distributedCache.Received(1).SetAsync(
            HaplotypeFrequencyCache.ActiveHaplotypeFrequencySetsDistributedCacheKey,
            Arg.Any<byte[]>(),
            TimeSpan.FromHours(24),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RefreshActiveHaplotypeFrequencySets_StoresTheSetsForTwentyFourHours()
    {
        var distributedCache = UnreachableDistributedCache();
        var instance = BuildInstance(distributedCache);
        frequencySetRepository.GetAllActiveSets().Returns([fixture.Create<Data.Models.HaplotypeFrequencySet>()]);

        await instance.RefreshActiveHaplotypeFrequencySets();

        await distributedCache.Received(1).SetAsync(
            HaplotypeFrequencyCache.ActiveHaplotypeFrequencySetsDistributedCacheKey,
            Arg.Any<byte[]>(),
            TimeSpan.FromHours(24),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RefreshActiveHaplotypeFrequencySets_WhenTheDistributedCacheDoesNotStoreTheSets_ReturnsFalse_AndThisInstanceStillReturnsTheNewSets()
    {
        var instance = BuildInstance(UnreachableDistributedCache());
        var newSet = fixture.Create<Data.Models.HaplotypeFrequencySet>();
        frequencySetRepository.GetAllActiveSets().Returns([newSet]);

        var stored = await instance.RefreshActiveHaplotypeFrequencySets();
        var result = await instance.GetActiveHaplotypeFrequencySets();

        stored.Should().BeFalse();
        result.Values.Should().ContainSingle().Which.Id.Should().Be(newSet.Id);
        // The read used the local entry that the refresh stored.
        await frequencySetRepository.Received(1).GetAllActiveSets();
    }

    [Test]
    public async Task GetAllHaplotypeFrequencies_MultipleCalls_QueriesRepositoryOnce()
    {
        const int setId = 1;
        frequencyRepository.GetAllHaplotypeFrequencies(setId).Returns([Record("a", "b", "c", "dqb1", "drb1", 0.5m)]);

        await sut.GetAllHaplotypeFrequencies(setId);
        await sut.GetAllHaplotypeFrequencies(setId);

        await frequencyRepository.Received(1).GetAllHaplotypeFrequencies(setId);
    }

    [Test]
    public async Task GetAllHaplotypeFrequencies_BuildsSetFrequenciesAndInternerFromRepository()
    {
        const int setId = 2;
        frequencyRepository.GetAllHaplotypeFrequencies(setId).Returns([Record("a", "b", "c", "dqb1", "drb1", 0.25m)]);

        var entry = await sut.GetAllHaplotypeFrequencies(setId);

        entry.SetFrequencies.Should().HaveCount(1);
        entry.Interner.TryResolve("a", "b", "c", "dqb1", "drb1", out var key).Should().BeTrue();
        entry.SetFrequencies[key].Frequency.Should().Be(0.25m);
    }

    [Test]
    public async Task GetAllHaplotypeFrequencies_WhenCapacityIsReached_EvictsTheLeastRecentlyUsedSet()
    {
        const int setIdA = 100;
        const int setIdB = 101;
        var limitedSut = BuildSut(awaitConsolidatedFrequencyWarm: false, cacheCapacityInBytes: OneRowSetSize);
        frequencyRepository.GetAllHaplotypeFrequencies(setIdA).Returns([Record("a", "b", "c", "dqb1", "drb1", 0.5m)]);
        frequencyRepository.GetAllHaplotypeFrequencies(setIdB).Returns([Record("a", "b", "c", "dqb1", "drb1", 0.6m)]);

        await limitedSut.GetAllHaplotypeFrequencies(setIdA);
        // Admitting B at a capacity of one set evicts A, the only (and so least-recently-used) tracked set.
        await limitedSut.GetAllHaplotypeFrequencies(setIdB);
        await limitedSut.GetAllHaplotypeFrequencies(setIdB);

        // B never had to be rebuilt a second time - it stayed resident.
        await frequencyRepository.Received(1).GetAllHaplotypeFrequencies(setIdB);

        // A had to be rebuilt from the database again, proving it was evicted rather than staying resident
        // indefinitely alongside every other set ever touched.
        await limitedSut.GetAllHaplotypeFrequencies(setIdA);
        await frequencyRepository.Received(2).GetAllHaplotypeFrequencies(setIdA);
    }

    [Test]
    public async Task GetAllHaplotypeFrequencies_ReAccessingAResidentSet_ProtectsItFromEviction()
    {
        const int setIdA = 200;
        const int setIdB = 201;
        const int setIdC = 202;
        var limitedSut = BuildSut(awaitConsolidatedFrequencyWarm: false, cacheCapacityInBytes: 2 * OneRowSetSize);
        frequencyRepository.GetAllHaplotypeFrequencies(setIdA).Returns([Record("a", "b", "c", "dqb1", "drb1", 0.5m)]);
        frequencyRepository.GetAllHaplotypeFrequencies(setIdB).Returns([Record("a", "b", "c", "dqb1", "drb1", 0.6m)]);
        frequencyRepository.GetAllHaplotypeFrequencies(setIdC).Returns([Record("a", "b", "c", "dqb1", "drb1", 0.7m)]);

        await limitedSut.GetAllHaplotypeFrequencies(setIdA);
        await limitedSut.GetAllHaplotypeFrequencies(setIdB);
        // Touching A again makes B the least-recently-used of the two, not A.
        await limitedSut.GetAllHaplotypeFrequencies(setIdA);
        // Admitting C at a capacity of two sets must therefore evict B, not A.
        await limitedSut.GetAllHaplotypeFrequencies(setIdC);

        await limitedSut.GetAllHaplotypeFrequencies(setIdA);
        await frequencyRepository.Received(1).GetAllHaplotypeFrequencies(setIdA);

        await limitedSut.GetAllHaplotypeFrequencies(setIdB);
        await frequencyRepository.Received(2).GetAllHaplotypeFrequencies(setIdB);
    }

    [Test]
    public async Task GetAllHaplotypeFrequencies_AfterCacheEntryExpiresIndependently_DoesNotEvictAStillResidentSetInItsPlace()
    {
        const int setIdA = 300;
        const int setIdB = 301;
        const int setIdC = 302;
        var (limitedSut, cacheProvider) = BuildSutWithCacheProvider(cacheCapacityInBytes: 2 * OneRowSetSize);
        frequencyRepository.GetAllHaplotypeFrequencies(setIdA).Returns([Record("a", "b", "c", "dqb1", "drb1", 0.5m)]);
        frequencyRepository.GetAllHaplotypeFrequencies(setIdB).Returns([Record("a", "b", "c", "dqb1", "drb1", 0.6m)]);
        frequencyRepository.GetAllHaplotypeFrequencies(setIdC).Returns([Record("a", "b", "c", "dqb1", "drb1", 0.7m)]);

        await limitedSut.GetAllHaplotypeFrequencies(setIdA);
        await limitedSut.GetAllHaplotypeFrequencies(setIdB);
        // A is now more-recently-used than B, so B alone is the tracker's least-recently-used entry.
        await limitedSut.GetAllHaplotypeFrequencies(setIdA);

        // Simulates the cache's own TTL expiring A independently of the tracker. The PostEvictionCallback registered on
        // A's entry must tell the tracker to forget it, freeing its bytes.
        cacheProvider.Cache.Remove(HaplotypeFrequencyCache.AllFrequenciesCacheKey(setIdA));
        // MemoryCache dispatches post-eviction callbacks on the thread pool rather than inline with Remove, so give
        // it a moment to actually run before relying on its effect below.
        await Task.Delay(200);

        // Admitting C at a capacity of two sets must not evict B, the only genuinely-resident set left. Without the
        // PostEvictionCallback fix, the tracker would still believe A holds its bytes and - being (mistakenly) more
        // recently used than B in its own bookkeeping - would evict B here, a set that was never actually expired.
        await limitedSut.GetAllHaplotypeFrequencies(setIdC);

        await limitedSut.GetAllHaplotypeFrequencies(setIdB);
        await frequencyRepository.Received(1).GetAllHaplotypeFrequencies(setIdB);
    }

    [Test]
    public async Task GetAllHaplotypeFrequencies_ManySmallSetsFitWhereOneLargeSetDoesNot()
    {
        var smallSetIds = fixture.CreateMany<int>(3).ToList();
        var largeSetId = fixture.Create<int>();
        var limitedSut = BuildSut(awaitConsolidatedFrequencyWarm: false, cacheCapacityInBytes: 3 * OneRowSetSize);
        foreach (var setId in smallSetIds)
        {
            frequencyRepository.GetAllHaplotypeFrequencies(setId).Returns(NewRows(setId, 1));
        }

        frequencyRepository.GetAllHaplotypeFrequencies(largeSetId).Returns(NewRows(largeSetId, 3));

        // Three sets of one row fit together: each is read once.
        foreach (var setId in smallSetIds.Concat(smallSetIds))
        {
            await limitedSut.GetAllHaplotypeFrequencies(setId);
        }

        // One set of three rows needs all the memory: the small sets go.
        await limitedSut.GetAllHaplotypeFrequencies(largeSetId);
        await limitedSut.GetAllHaplotypeFrequencies(smallSetIds[0]);

        await frequencyRepository.Received(2).GetAllHaplotypeFrequencies(smallSetIds[0]);
        await frequencyRepository.Received(1).GetAllHaplotypeFrequencies(smallSetIds[1]);
        await frequencyRepository.Received(1).GetAllHaplotypeFrequencies(smallSetIds[2]);
    }

    [Test]
    public async Task GetAllHaplotypeFrequencies_ASetInUse_DoesNotExpire()
    {
        var setId = fixture.Create<int>();
        frequencyRepository.GetAllHaplotypeFrequencies(setId).Returns(NewRows(setId, 1));

        await sut.GetAllHaplotypeFrequencies(setId);
        MoveClockTo(SetLocalEntryLifetime - ClockMargin);
        await sut.GetAllHaplotypeFrequencies(setId);
        // Past the expiry from the load, but not from the last use.
        MoveClockTo(2 * (SetLocalEntryLifetime - ClockMargin));
        await sut.GetAllHaplotypeFrequencies(setId);

        await frequencyRepository.Received(1).GetAllHaplotypeFrequencies(setId);
    }

    [Test]
    public async Task GetAllHaplotypeFrequencies_ASetNotUsedForTheExpiry_IsLoadedAgain()
    {
        var setId = fixture.Create<int>();
        frequencyRepository.GetAllHaplotypeFrequencies(setId).Returns(NewRows(setId, 1));

        await sut.GetAllHaplotypeFrequencies(setId);
        MoveClockTo(SetLocalEntryLifetime + ClockMargin);
        await sut.GetAllHaplotypeFrequencies(setId);

        await frequencyRepository.Received(2).GetAllHaplotypeFrequencies(setId);
    }

    // ---- The distributed cache -------------------------------------------------------------------------------------

    [Test]
    public async Task GetAllHaplotypeFrequencies_WhenAnotherInstanceLoadedTheSet_DoesNotReadTheFrequenciesFromTheDatabase()
    {
        var store = DistributedCacheBuilder.NewSharedStore();
        var instanceA = BuildInstance(DistributedCacheBuilder.NewProviderOver(store));
        var instanceB = BuildInstance(DistributedCacheBuilder.NewProviderOver(store));
        var set = NewSet();
        var rows = NewRows(set.Id, fixture.Create<int>());
        frequencyRepository.GetAllHaplotypeFrequencies(set.Id).Returns(rows);

        await instanceA.GetAllHaplotypeFrequencies(set.Id);
        var entryOfB = await instanceB.GetAllHaplotypeFrequencies(set.Id);

        entryOfB.SetFrequencies.Should().HaveCount(rows.Count);
        await frequencyRepository.Received(1).GetAllHaplotypeFrequencies(set.Id);
    }

    /// <summary>
    /// No behaviour change: the order of the rows sets the order of the projected pool, which is the tie-break of
    /// truncation when frequencies are equal.
    /// </summary>
    [Test]
    public async Task GetAllHaplotypeFrequencies_FromTheDistributedCache_BuildsTheSameEntryAsFromTheDatabase()
    {
        var store = DistributedCacheBuilder.NewSharedStore();
        var instanceA = BuildInstance(DistributedCacheBuilder.NewProviderOver(store));
        var instanceB = BuildInstance(DistributedCacheBuilder.NewProviderOver(store));
        var set = NewSet();
        frequencyRepository.GetAllHaplotypeFrequencies(set.Id).Returns(RowsWithOneFrequency(set.Id));

        var entryFromTheDatabase = await instanceA.GetAllHaplotypeFrequencies(set.Id);
        var entryFromTheDistributedCache = await instanceB.GetAllHaplotypeFrequencies(set.Id);

        await frequencyRepository.Received(1).GetAllHaplotypeFrequencies(set.Id);
        entryFromTheDistributedCache.ShouldBeTheSameEntryAs(entryFromTheDatabase);
    }

    [Test]
    public async Task GetAllHaplotypeFrequencies_OnAMiss_StoresTheSetForThirtyDays()
    {
        var distributedCache = DistributedCacheThatAcceptsEachWrite();
        var instance = BuildInstance(distributedCache);
        var set = NewSet();
        frequencyRepository.GetAllHaplotypeFrequencies(set.Id).Returns(NewRows(set.Id, 1));

        await instance.GetAllHaplotypeFrequencies(set.Id);

        await distributedCache.Received(1).SetAsync(
            HaplotypeFrequencyCache.FrequencySetDistributedCacheKey(set),
            Arg.Any<byte[]>(),
            TimeSpan.FromDays(30),
            Arg.Any<CancellationToken>());
    }

    /// <summary>An inactive set can be the set of an import that still runs, with only part of its rows.</summary>
    [Test]
    public async Task GetAllHaplotypeFrequencies_ForASetThatIsNotActive_DoesNotStoreTheSet()
    {
        var distributedCache = DistributedCacheThatAcceptsEachWrite();
        var instance = BuildInstance(distributedCache);
        var set = NewSet(active: false);
        frequencyRepository.GetAllHaplotypeFrequencies(set.Id).Returns(NewRows(set.Id, 1));

        await instance.GetAllHaplotypeFrequencies(set.Id);

        await distributedCache.DidNotReceiveWithAnyArgs().SetAsync(default, default, default, default);
    }

    [Test]
    public async Task GetAllHaplotypeFrequencies_ForASetThatDoesNotExist_DoesNotUseTheDistributedCache()
    {
        var distributedCache = DistributedCacheThatAcceptsEachWrite();
        var instance = BuildInstance(distributedCache);
        var setId = fixture.Create<int>();
        frequencySetRepository.GetSet(setId).Returns((Data.Models.HaplotypeFrequencySet)null);
        frequencyRepository.GetAllHaplotypeFrequencies(setId).Returns(new List<LightweightHaplotypeFrequencyRecord>());

        var entry = await instance.GetAllHaplotypeFrequencies(setId);

        entry.SetFrequencies.Should().BeEmpty();
        distributedCache.ReceivedCalls().Should().BeEmpty();
    }

    /// <summary>Set ids are SQL identity values: after a restore of the database, the same id can name another set.</summary>
    [Test]
    public async Task GetAllHaplotypeFrequencies_ForASetWithAnotherImportTime_DoesNotUseTheStoredSet()
    {
        var store = DistributedCacheBuilder.NewSharedStore();
        var instanceA = BuildInstance(DistributedCacheBuilder.NewProviderOver(store));
        var instanceB = BuildInstance(DistributedCacheBuilder.NewProviderOver(store));
        var set = NewSet();
        var rowsOfTheRestoredSet = NewRows(set.Id, 1);
        frequencyRepository.GetAllHaplotypeFrequencies(set.Id).Returns(NewRows(set.Id, 1), rowsOfTheRestoredSet);
        await instanceA.GetAllHaplotypeFrequencies(set.Id);

        frequencySetRepository.GetSet(set.Id).Returns(ReplacingSet(set));
        var entryOfB = await instanceB.GetAllHaplotypeFrequencies(set.Id);

        var row = rowsOfTheRestoredSet.Single();
        entryOfB.Interner.TryResolve(row.A, row.B, row.C, row.DQB1, row.DRB1, out _).Should().BeTrue();
        await frequencyRepository.Received(2).GetAllHaplotypeFrequencies(set.Id);
    }

    [Test]
    public async Task GetAllHaplotypeFrequencies_WhenTheStoredSetCannotBeDecoded_ReadsTheDatabaseAndStoresTheSetAgain()
    {
        var store = DistributedCacheBuilder.NewSharedStore();
        var set = NewSet();
        var rows = NewRows(set.Id, fixture.Create<int>());
        frequencyRepository.GetAllHaplotypeFrequencies(set.Id).Returns(rows);
        await DistributedCacheBuilder.NewProviderOver(store)
            .SetChunkedAsync(HaplotypeFrequencyCache.FrequencySetDistributedCacheKey(set), fixture.Create<byte[]>(), TimeSpan.FromDays(1));

        var entry = await BuildInstance(DistributedCacheBuilder.NewProviderOver(store)).GetAllHaplotypeFrequencies(set.Id);
        var entryOfAnotherInstance = await BuildInstance(DistributedCacheBuilder.NewProviderOver(store)).GetAllHaplotypeFrequencies(set.Id);

        entry.SetFrequencies.Should().HaveCount(rows.Count);
        entryOfAnotherInstance.SetFrequencies.Should().HaveCount(rows.Count);
        // The other instance used the set that the first stored again.
        await frequencyRepository.Received(1).GetAllHaplotypeFrequencies(set.Id);
        logger.Received(1).SendTrace(Arg.Is<string>(message => message.Contains("cannot be decoded")), LogLevel.Warn);
    }

    [Test]
    public async Task GetAllHaplotypeFrequencies_WhenTheDistributedCacheCannotBeReached_ReadsTheDatabase()
    {
        var instance = BuildInstance(UnreachableDistributedCache());
        var set = NewSet();
        var rows = NewRows(set.Id, fixture.Create<int>());
        frequencyRepository.GetAllHaplotypeFrequencies(set.Id).Returns(rows);

        var entry = await instance.GetAllHaplotypeFrequencies(set.Id);

        entry.SetFrequencies.Should().HaveCount(rows.Count);
    }

    [Test]
    public async Task GetAllHaplotypeFrequencies_ConcurrentCallsForOneSetWhenTheDistributedCacheMisses_ReadTheDatabaseOnce()
    {
        var instance = BuildInstance(UnreachableDistributedCache());
        var set = NewSet();
        var databaseRead = new TaskCompletionSource<List<LightweightHaplotypeFrequencyRecord>>();
        frequencyRepository.GetAllHaplotypeFrequencies(set.Id).Returns(databaseRead.Task);

        var calls = Enumerable.Range(0, 10).Select(_ => instance.GetAllHaplotypeFrequencies(set.Id)).ToList();
        databaseRead.SetResult(NewRows(set.Id, 1));
        await Task.WhenAll(calls);

        await frequencyRepository.Received(1).GetAllHaplotypeFrequencies(set.Id);
    }

    [Test]
    public async Task GetAllHaplotypeFrequencies_WithNoDistributedCache_ReadsTheDatabase_AndDoesNotUseTheDistributedCache()
    {
        var distributedCache = Substitute.For<IDistributedCacheProvider>();
        distributedCache.IsEnabled.Returns(false);
        var instance = BuildInstance(distributedCache);
        var set = NewSet();
        var rows = NewRows(set.Id, fixture.Create<int>());
        frequencyRepository.GetAllHaplotypeFrequencies(set.Id).Returns(rows);

        var entry = await instance.GetAllHaplotypeFrequencies(set.Id);

        entry.SetFrequencies.Should().HaveCount(rows.Count);
        await distributedCache.DidNotReceiveWithAnyArgs().GetAsync(default);
        await distributedCache.DidNotReceiveWithAnyArgs().SetAsync(default, default, default, default);
    }

    // ---- The check of a loaded set against its row -----------------------------------------------------------------

    [Test]
    public async Task GetAllHaplotypeFrequencies_WithinTheCheckInterval_DoesNotReadTheSetRowAgain()
    {
        var set = NewSet();
        frequencyRepository.GetAllHaplotypeFrequencies(set.Id).Returns(NewRows(set.Id, 1));

        await sut.GetAllHaplotypeFrequencies(set.Id);
        MoveClockTo(SetCheckInterval - ClockMargin);
        await sut.GetAllHaplotypeFrequencies(set.Id);

        // The read of the load only.
        await frequencySetRepository.Received(1).GetSet(set.Id);
    }

    [Test]
    public async Task GetAllHaplotypeFrequencies_AfterTheCheckInterval_KeepsASetThatIsUnchanged()
    {
        var set = NewSet();
        frequencyRepository.GetAllHaplotypeFrequencies(set.Id).Returns(NewRows(set.Id, 1));

        await sut.GetAllHaplotypeFrequencies(set.Id);
        MoveClockTo(SetCheckInterval + ClockMargin);
        await sut.GetAllHaplotypeFrequencies(set.Id);

        // The read of the load, and the read of the check.
        await frequencySetRepository.Received(2).GetSet(set.Id);
        await frequencyRepository.Received(1).GetAllHaplotypeFrequencies(set.Id);
    }

    /// <summary>
    /// Set ids are SQL identity values: after a restore or a copy of the database, the id of a set that a process holds can
    /// name another set. A set in use does not expire, so only the check replaces it.
    /// </summary>
    [Test]
    public async Task GetAllHaplotypeFrequencies_AfterTheCheckInterval_LoadsASetThatTheDatabaseReplaced()
    {
        var set = NewSet();
        var rowsOfTheNewSet = NewRows(set.Id, 1);
        frequencyRepository.GetAllHaplotypeFrequencies(set.Id).Returns(NewRows(set.Id, 1), rowsOfTheNewSet);
        await sut.GetAllHaplotypeFrequencies(set.Id);

        frequencySetRepository.GetSet(set.Id).Returns(ReplacingSet(set));
        MoveClockTo(SetCheckInterval + ClockMargin);
        var entry = await sut.GetAllHaplotypeFrequencies(set.Id);

        var row = rowsOfTheNewSet.Single();
        entry.SetFrequencies.Should().ContainSingle();
        entry.Interner.TryResolve(row.A, row.B, row.C, row.DQB1, row.DRB1, out _).Should().BeTrue();
    }

    [Test]
    public async Task GetAllHaplotypeFrequencies_AfterTheCheckInterval_LoadsAgainASetWhoseRowIsGone()
    {
        var set = NewSet();
        frequencyRepository.GetAllHaplotypeFrequencies(set.Id).Returns(NewRows(set.Id, 1), NewRows(set.Id, 0));
        await sut.GetAllHaplotypeFrequencies(set.Id);

        frequencySetRepository.GetSet(set.Id).Returns((Data.Models.HaplotypeFrequencySet)null);
        MoveClockTo(SetCheckInterval + ClockMargin);
        var entry = await sut.GetAllHaplotypeFrequencies(set.Id);

        entry.SetFrequencies.Should().BeEmpty();
    }

    /// <summary>A loaded set needs no database: a fault of the check does not fail the caller.</summary>
    [Test]
    public async Task GetAllHaplotypeFrequencies_WhenTheCheckCannotReadTheSetRow_KeepsTheSet_AndLogsAWarning()
    {
        var set = NewSet();
        var rows = NewRows(set.Id, fixture.Create<int>());
        frequencyRepository.GetAllHaplotypeFrequencies(set.Id).Returns(rows);
        await sut.GetAllHaplotypeFrequencies(set.Id);

        frequencySetRepository.GetSet(set.Id).ThrowsAsync(new InvalidOperationException(fixture.Create<string>()));
        MoveClockTo(SetCheckInterval + ClockMargin);
        var entry = await sut.GetAllHaplotypeFrequencies(set.Id);

        entry.SetFrequencies.Should().HaveCount(rows.Count);
        await frequencyRepository.Received(1).GetAllHaplotypeFrequencies(set.Id);
        logger.Received(1).SendTrace(Arg.Is<string>(message => message.Contains("could not be checked")), LogLevel.Warn);
    }

    /// <summary>One caller evicts the replaced set, and the others wait for the one load of the new set.</summary>
    [Test]
    public async Task GetAllHaplotypeFrequencies_ConcurrentCallsAfterTheCheckInterval_ReadTheSetRowOnce_AndLoadTheReplacedSetOnce()
    {
        var set = NewSet();
        frequencyRepository.GetAllHaplotypeFrequencies(set.Id).Returns(NewRows(set.Id, 1), NewRows(set.Id, 1));
        await sut.GetAllHaplotypeFrequencies(set.Id);

        var newSet = ReplacingSet(set);
        var rowReadOfTheCheck = new TaskCompletionSource<Data.Models.HaplotypeFrequencySet>();
        frequencySetRepository.GetSet(set.Id).Returns(rowReadOfTheCheck.Task, Task.FromResult(newSet));
        MoveClockTo(SetCheckInterval + ClockMargin);

        var calls = Enumerable.Range(0, 10).Select(_ => sut.GetAllHaplotypeFrequencies(set.Id)).ToList();
        rowReadOfTheCheck.SetResult(newSet);
        await Task.WhenAll(calls);

        // The read of the first load, the read of the check, and the read of the load of the new set.
        await frequencySetRepository.Received(3).GetSet(set.Id);
        await frequencyRepository.Received(2).GetAllHaplotypeFrequencies(set.Id);
    }

    [Test]
    public async Task GetAllHaplotypeFrequencies_KicksOffConsolidationWarmingForTheSameEntryInstance()
    {
        const int setId = 3;
        frequencyRepository.GetAllHaplotypeFrequencies(setId).Returns([Record("a", "b", "c", "dqb1", "drb1", 0.5m)]);

        FrequencySetCacheEntry warmedEntry = null;
        frequencyConsolidator.PreConsolidateFrequenciesForCommonMissingLoci(Arg.Any<FrequencySetCacheEntry>())
            .Returns(ci =>
            {
                warmedEntry = ci.Arg<FrequencySetCacheEntry>();
                return FrozenDictionary<HaplotypeKey, decimal>.Empty;
            });

        var entry = await sut.GetAllHaplotypeFrequencies(setId);
        await WaitUntil(() => warmedEntry != null);

        // Warming operates on the very instance that was cached - that is what guarantees a single shared interner and lifetime.
        warmedEntry.Should().BeSameAs(entry);
    }

    [Test]
    public async Task GetConsolidatedFrequency_BeforeWarmingCompletes_ReturnsDirectlyCalculatedValue()
    {
        const int setId = 4;
        const decimal expectedFrequency = 0.1234m;
        frequencyRepository.GetAllHaplotypeFrequencies(setId).Returns(new List<LightweightHaplotypeFrequencyRecord>());
        // Leaving PreConsolidate... unconfigured returns null, so ConsolidatedFrequencies stays null and the direct path is taken.
        frequencyConsolidator.ConsolidateFrequenciesForHaplotype(
                Arg.Any<FrequencySetCacheEntry>(),
                Arg.Any<HfSetHaplotypeNames>(),
                Arg.Any<ISet<Locus>>())
            .Returns(expectedFrequency);

        var hla = new HfSetHaplotypeNames(valueA: "a", valueB: "b", valueC: "c", valueDqb1: "dqb1", valueDrb1: "drb1");

        var result = await sut.GetConsolidatedFrequency(setId, hla, new HashSet<Locus> { Locus.C });

        result.Should().Be(expectedFrequency);
    }

    [Test]
    public async Task GetConsolidatedFrequency_AfterWarmingCompletes_ReadsFromConsolidatedCollection()
    {
        const int setId = 5;
        const decimal expectedFrequency = 0.42m;
        var hla = new HfSetHaplotypeNames(valueA: "a", valueB: "b", valueC: "c", valueDqb1: "dqb1", valueDrb1: "drb1");
        var excludedLoci = new HashSet<Locus> { Locus.C };

        frequencyRepository.GetAllHaplotypeFrequencies(setId).Returns([Record("a", "b", "c", "dqb1", "drb1", expectedFrequency)]);

        // Produce a consolidated collection keyed exactly as the read path will look it up (same interner, C removed).
        frequencyConsolidator.PreConsolidateFrequenciesForCommonMissingLoci(Arg.Any<FrequencySetCacheEntry>())
            .Returns(ci =>
            {
                var entry = ci.Arg<FrequencySetCacheEntry>();
                var key = entry.Interner.ConvertWherePossible("a", "b", "c", "dqb1", "drb1").RemoveLoci([Locus.C]);
                return new Dictionary<HaplotypeKey, decimal> { [key] = expectedFrequency }.ToFrozenDictionary();
            });

        await sut.GetAllHaplotypeFrequencies(setId);
        await WaitForConsolidation(setId);

        var result = await sut.GetConsolidatedFrequency(setId, hla, excludedLoci);

        result.Should().Be(expectedFrequency);
        // Once warmed, the value is read from the collection - no per-haplotype direct calculation.
        frequencyConsolidator.DidNotReceive().ConsolidateFrequenciesForHaplotype(
            Arg.Any<FrequencySetCacheEntry>(), Arg.Any<HfSetHaplotypeNames>(), Arg.Any<ISet<Locus>>());
    }

    // ---- AwaitConsolidatedFrequencyWarm ----------------------------------------------------------------------------

    [Test]
    public async Task GetAllHaplotypeFrequencies_WhenAwaitingTheWarm_ReturnsWithConsolidatedFrequenciesAlreadyPopulated()
    {
        const int setId = 6;
        var awaitingSut = BuildSut(awaitConsolidatedFrequencyWarm: true);
        frequencyRepository.GetAllHaplotypeFrequencies(setId).Returns([Record("a", "b", "c", "dqb1", "drb1", 0.5m)]);
        frequencyConsolidator.PreConsolidateFrequenciesForCommonMissingLoci(Arg.Any<FrequencySetCacheEntry>())
            .Returns(FrozenDictionary<HaplotypeKey, decimal>.Empty);

        var entry = await awaitingSut.GetAllHaplotypeFrequencies(setId);

        // No WaitUntil, deliberately: the point of the setting is that this holds the instant the call returns, so a
        // poll here would hide the very thing being asserted.
        entry.ConsolidatedFrequencies.Should().NotBeNull();
    }

    [Test]
    public async Task GetConsolidatedFrequency_WhenAwaitingTheWarm_NeverFallsBackToADirectScan()
    {
        const int setId = 7;
        const decimal expectedFrequency = 0.77m;
        var awaitingSut = BuildSut(awaitConsolidatedFrequencyWarm: true);
        var hla = new HfSetHaplotypeNames(valueA: "a", valueB: "b", valueC: "c", valueDqb1: "dqb1", valueDrb1: "drb1");

        frequencyRepository.GetAllHaplotypeFrequencies(setId).Returns([Record("a", "b", "c", "dqb1", "drb1", expectedFrequency)]);
        frequencyConsolidator.PreConsolidateFrequenciesForCommonMissingLoci(Arg.Any<FrequencySetCacheEntry>())
            .Returns(ci =>
            {
                var entry = ci.Arg<FrequencySetCacheEntry>();
                var key = entry.Interner.ConvertWherePossible("a", "b", "c", "dqb1", "drb1").RemoveLoci([Locus.C]);
                return new Dictionary<HaplotypeKey, decimal> { [key] = expectedFrequency }.ToFrozenDictionary();
            });

        // The very first consolidated read of the set - which is exactly the one that loses the race when the warm is
        // not awaited, and which costs a full per-haplotype scan rather than a dictionary read.
        var result = await awaitingSut.GetConsolidatedFrequency(setId, hla, new HashSet<Locus> { Locus.C });

        result.Should().Be(expectedFrequency);
        frequencyConsolidator.DidNotReceive().ConsolidateFrequenciesForHaplotype(
            Arg.Any<FrequencySetCacheEntry>(), Arg.Any<HfSetHaplotypeNames>(), Arg.Any<ISet<Locus>>());
    }

    [Test]
    public async Task GetAllHaplotypeFrequencies_WhenAwaitingTheWarm_ConsolidatesOncePerSet()
    {
        const int setId = 8;
        var awaitingSut = BuildSut(awaitConsolidatedFrequencyWarm: true);
        frequencyRepository.GetAllHaplotypeFrequencies(setId).Returns([Record("a", "b", "c", "dqb1", "drb1", 0.5m)]);
        frequencyConsolidator.PreConsolidateFrequenciesForCommonMissingLoci(Arg.Any<FrequencySetCacheEntry>())
            .Returns(FrozenDictionary<HaplotypeKey, decimal>.Empty);

        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => awaitingSut.GetAllHaplotypeFrequencies(setId)));

        // The standing hazard is a second writer of ConsolidatedFrequencies. Awaiting inside the GetOrAddAsync
        // factory is what prevents it: concurrent callers share one lazy task rather than each starting a warm.
        frequencyConsolidator.Received(1).PreConsolidateFrequenciesForCommonMissingLoci(Arg.Any<FrequencySetCacheEntry>());
    }

    [Test]
    public async Task GetAllHaplotypeFrequencies_WhenAwaitingTheWarm_AndPreConsolidationThrows_PropagatesTheFailure()
    {
        const int setId = 9;
        var awaitingSut = BuildSut(awaitConsolidatedFrequencyWarm: true);
        frequencyRepository.GetAllHaplotypeFrequencies(setId).Returns([Record("a", "b", "c", "dqb1", "drb1", 0.5m)]);
        frequencyConsolidator.PreConsolidateFrequenciesForCommonMissingLoci(Arg.Any<FrequencySetCacheEntry>())
            .Throws(new InvalidOperationException("boom"));

        // Awaiting the warm exists precisely so a caller never gets back an entry whose ConsolidatedFrequencies is
        // silently stuck null - a failed pre-consolidation must surface here, not be swallowed.
        var act = () => awaitingSut.GetAllHaplotypeFrequencies(setId);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Test]
    public async Task GetAllHaplotypeFrequencies_WhenNotAwaitingTheWarm_AndPreConsolidationThrows_LogsAndReturnsUnwarmedEntry()
    {
        const int setId = 10;
        frequencyRepository.GetAllHaplotypeFrequencies(setId).Returns([Record("a", "b", "c", "dqb1", "drb1", 0.5m)]);
        frequencyConsolidator.PreConsolidateFrequenciesForCommonMissingLoci(Arg.Any<FrequencySetCacheEntry>())
            .Throws(new InvalidOperationException("boom"));

        // The background path has no awaiter to propagate to, so the failure must be caught and logged rather than
        // faulting an unobserved task - the caller still gets an entry back, just an unwarmed one.
        var entry = await sut.GetAllHaplotypeFrequencies(setId);
        await WaitUntil(() => logger.ReceivedCalls().Any());

        entry.ConsolidatedFrequencies.Should().BeNull();
        logger.Received(1).SendTrace(Arg.Is<string>(msg => msg.Contains("Failed to warm consolidated frequency cache")), LogLevel.Error);
    }

    private async Task WaitForConsolidation(int setId) =>
        await WaitUntil(async () => (await sut.GetAllHaplotypeFrequencies(setId)).ConsolidatedFrequencies != null);

    private static async Task WaitUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(10);
        }

        throw new TimeoutException("Condition was not met within the allotted time.");
    }

    private static async Task WaitUntil(Func<Task<bool>> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(10);
        }

        throw new TimeoutException("Condition was not met within the allotted time.");
    }

    /// <summary>A distributed cache that cannot be reached: every read misses, and no write is stored.</summary>
    private static IDistributedCacheProvider UnreachableDistributedCache()
    {
        var distributedCache = Substitute.For<IDistributedCacheProvider>();
        distributedCache.IsEnabled.Returns(true);
        distributedCache.GetAsync(default).ReturnsForAnyArgs((byte[])null);
        distributedCache.SetAsync(default, null, TimeSpan.Zero).ReturnsForAnyArgs(false);
        return distributedCache;
    }

    /// <summary>A distributed cache that accepts each write, and so lets a test see the writes: every read misses.</summary>
    private static IDistributedCacheProvider DistributedCacheThatAcceptsEachWrite()
    {
        var distributedCache = Substitute.For<IDistributedCacheProvider>();
        distributedCache.IsEnabled.Returns(true);
        distributedCache.GetAsync(default).ReturnsForAnyArgs((byte[])null);
        distributedCache.SetAsync(default, null, TimeSpan.Zero).ReturnsForAnyArgs(true);
        return distributedCache;
    }

    /// <summary>A set in the database: <see cref="IHaplotypeFrequencySetRepository.GetSet"/> returns it.</summary>
    private Data.Models.HaplotypeFrequencySet NewSet(bool active = true)
    {
        var set = fixture.Build<Data.Models.HaplotypeFrequencySet>().With(x => x.Active, active).Create();
        frequencySetRepository.GetSet(set.Id).Returns(set);
        return set;
    }

    /// <summary>
    /// The set that the id of <paramref name="set"/> names after a restore or a copy of the database: another set, with
    /// another import time.
    /// </summary>
    private Data.Models.HaplotypeFrequencySet ReplacingSet(Data.Models.HaplotypeFrequencySet set) =>
        fixture.Build<Data.Models.HaplotypeFrequencySet>()
            .With(x => x.Id, set.Id)
            .With(x => x.Active, true)
            .With(x => x.DateTimeAdded, set.DateTimeAdded + TimeSpan.FromMinutes(fixture.Create<int>()))
            .Create();

    private List<LightweightHaplotypeFrequencyRecord> NewRows(int setId, int rowCount) =>
        fixture.Build<LightweightHaplotypeFrequencyRecord>().With(x => x.SetId, setId).CreateMany(rowCount).ToList();

    /// <summary>
    /// Rows over a few names at each locus, in a random order, so that the interner ids depend on the order of the rows.
    /// One frequency for all, as in a tie-break of truncation, with the scale of SQL's decimal(20,20); and each typing
    /// category.
    /// </summary>
    private List<LightweightHaplotypeFrequencyRecord> RowsWithOneFrequency(int setId)
    {
        const int namesAtEachLocus = 3;
        var namesByLocus = Enumerable.Range(0, 5).Select(_ => fixture.CreateMany<string>(namesAtEachLocus).ToArray()).ToArray();
        var frequency = new decimal(fixture.Create<int>(), 0, 0, false, 20);
        var categories = Enum.GetValues<HaplotypeTypingCategory>();
        var random = new Random(fixture.Create<int>());

        var haplotypes =
            from a in namesByLocus[0]
            from b in namesByLocus[1]
            from c in namesByLocus[2]
            from dqb1 in namesByLocus[3]
            from drb1 in namesByLocus[4]
            select (a, b, c, dqb1, drb1);

        return haplotypes
            .OrderBy(_ => random.Next())
            .Select((haplotype, index) => new LightweightHaplotypeFrequencyRecord
            {
                A = haplotype.a,
                B = haplotype.b,
                C = haplotype.c,
                DQB1 = haplotype.dqb1,
                DRB1 = haplotype.drb1,
                SetId = setId,
                Frequency = frequency,
                TypingCategory = categories[index % categories.Length]
            })
            .ToList();
    }


    private static LightweightHaplotypeFrequencyRecord Record(string a, string b, string c, string dqb1, string drb1, decimal frequency) =>
        new()
        {
            A = a,
            B = b,
            C = c,
            DQB1 = dqb1,
            DRB1 = drb1,
            Frequency = frequency,
            TypingCategory = default
        };
}
