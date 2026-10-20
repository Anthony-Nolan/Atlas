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
using AutoFixture;
using AwesomeAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;
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

    private HaplotypeFrequencyCache BuildSut(bool awaitConsolidatedFrequencyWarm, int maxCachedFrequencySets = 1000) =>
        BuildSutWithCacheProvider(awaitConsolidatedFrequencyWarm, maxCachedFrequencySets).Sut;

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
        int maxCachedFrequencySets = 1000,
        IDistributedCacheProvider distributedCache = null)
    {
        var cacheSettings = fixture.Build<HaplotypeFrequencySetCacheSettings>()
            .With(x => x.ActiveSetCacheExpiryMinutes, ActiveSetCacheExpiryMinutes)
            .With(x => x.MaxCachedFrequencySets, maxCachedFrequencySets)
            .With(x => x.SetCacheExpiryMinutes, 60)
            .With(x => x.AwaitConsolidatedFrequencyWarm, awaitConsolidatedFrequencyWarm)
            .Create();

        var cacheProvider = new HaplotypeFrequencySetCacheProvider(
            AppCacheBuilder.NewCacheOver(new MemoryCache(new MemoryCacheOptions { Clock = clock })));
        var residencyTracker = new FrequencySetResidencyTracker(
            maxCachedFrequencySets,
            evictedSetId => cacheProvider.Cache.Remove(HaplotypeFrequencyCache.AllFrequenciesCacheKey(evictedSetId))
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
        var limitedSut = BuildSut(awaitConsolidatedFrequencyWarm: false, maxCachedFrequencySets: 1);
        frequencyRepository.GetAllHaplotypeFrequencies(setIdA).Returns([Record("a", "b", "c", "dqb1", "drb1", 0.5m)]);
        frequencyRepository.GetAllHaplotypeFrequencies(setIdB).Returns([Record("a", "b", "c", "dqb1", "drb1", 0.6m)]);

        await limitedSut.GetAllHaplotypeFrequencies(setIdA);
        // Admitting B at capacity 1 evicts A, the only (and so least-recently-used) tracked set.
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
        var limitedSut = BuildSut(awaitConsolidatedFrequencyWarm: false, maxCachedFrequencySets: 2);
        frequencyRepository.GetAllHaplotypeFrequencies(setIdA).Returns([Record("a", "b", "c", "dqb1", "drb1", 0.5m)]);
        frequencyRepository.GetAllHaplotypeFrequencies(setIdB).Returns([Record("a", "b", "c", "dqb1", "drb1", 0.6m)]);
        frequencyRepository.GetAllHaplotypeFrequencies(setIdC).Returns([Record("a", "b", "c", "dqb1", "drb1", 0.7m)]);

        await limitedSut.GetAllHaplotypeFrequencies(setIdA);
        await limitedSut.GetAllHaplotypeFrequencies(setIdB);
        // Touching A again makes B the least-recently-used of the two, not A.
        await limitedSut.GetAllHaplotypeFrequencies(setIdA);
        // Admitting C at capacity 2 must therefore evict B, not A.
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
        var (limitedSut, cacheProvider) = BuildSutWithCacheProvider(maxCachedFrequencySets: 2);
        frequencyRepository.GetAllHaplotypeFrequencies(setIdA).Returns([Record("a", "b", "c", "dqb1", "drb1", 0.5m)]);
        frequencyRepository.GetAllHaplotypeFrequencies(setIdB).Returns([Record("a", "b", "c", "dqb1", "drb1", 0.6m)]);
        frequencyRepository.GetAllHaplotypeFrequencies(setIdC).Returns([Record("a", "b", "c", "dqb1", "drb1", 0.7m)]);

        await limitedSut.GetAllHaplotypeFrequencies(setIdA);
        await limitedSut.GetAllHaplotypeFrequencies(setIdB);
        // A is now more-recently-used than B, so B alone is the tracker's least-recently-used entry.
        await limitedSut.GetAllHaplotypeFrequencies(setIdA);

        // Simulates the cache's own TTL expiring A independently of the tracker - which can happen to any entry
        // regardless of LRU order, since expiry is absolute-time-based, not usage-based. The PostEvictionCallback
        // registered on A's entry must tell the tracker to forget it, freeing its slot.
        cacheProvider.Cache.Remove(HaplotypeFrequencyCache.AllFrequenciesCacheKey(setIdA));
        // MemoryCache dispatches post-eviction callbacks on the thread pool rather than inline with Remove, so give
        // it a moment to actually run before relying on its effect below.
        await Task.Delay(200);

        // Admitting C at capacity 2 must not evict B, the only genuinely-resident set left. Without the
        // PostEvictionCallback fix, the tracker would still believe A occupies a slot and - being (mistakenly) more
        // recently used than B in its own bookkeeping - would evict B here, a set that was never actually expired.
        await limitedSut.GetAllHaplotypeFrequencies(setIdC);

        await limitedSut.GetAllHaplotypeFrequencies(setIdB);
        await frequencyRepository.Received(1).GetAllHaplotypeFrequencies(setIdB);
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
        distributedCache.GetAsync(default).ReturnsForAnyArgs((byte[])null);
        distributedCache.SetAsync(default, null, TimeSpan.Zero).ReturnsForAnyArgs(false);
        return distributedCache;
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
