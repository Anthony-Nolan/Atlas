using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
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
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using HfSetHaplotypeNames = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.LociInfo<string>;

namespace Atlas.MatchPrediction.Test.Services.HaplotypeFrequencies;

[TestFixture]
internal class HaplotypeFrequencyCacheTests
{
    private IHaplotypeFrequenciesRepository frequencyRepository;
    private IHaplotypeFrequencySetRepository frequencySetRepository;
    private IFrequencyConsolidator frequencyConsolidator;
    private IMatchPredictionLogger<MatchProbabilityLoggingContext> logger;

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

        // Pinned rather than left to AutoFixture: AwaitConsolidatedFrequencyWarm decides whether the pre-consolidation
        // is on the critical path, so every test below would otherwise be exercising whichever mode the fixture picked.
        sut = BuildSut(awaitConsolidatedFrequencyWarm: false);
    }

    private HaplotypeFrequencyCache BuildSut(bool awaitConsolidatedFrequencyWarm)
    {
        var cacheSettings = fixture.Build<HaplotypeFrequencySetCacheSettings>()
            .With(x => x.ActiveSetCacheExpiryMinutes, 5)
            .With(x => x.AwaitConsolidatedFrequencyWarm, awaitConsolidatedFrequencyWarm)
            .Create();

        return new HaplotypeFrequencyCache(
            AppCacheBuilder.NewPersistentCacheProvider(),
            frequencyRepository,
            frequencySetRepository,
            frequencyConsolidator,
            logger,
            Options.Create(cacheSettings)
        );
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
    public async Task RemoveActiveHaplotypeFrequencySets_CausesRepositoryToBeReQueried()
    {
        var firstActiveSet = fixture.Create<Data.Models.HaplotypeFrequencySet>();
        var secondActiveSet = fixture.Create<Data.Models.HaplotypeFrequencySet>();
        frequencySetRepository.GetAllActiveSets().Returns([firstActiveSet], [secondActiveSet]);

        await sut.GetActiveHaplotypeFrequencySets();
        sut.RemoveActiveHaplotypeFrequencySets();
        await sut.GetActiveHaplotypeFrequencySets();

        await frequencySetRepository.Received(2).GetAllActiveSets();
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
        await awaitingSut.Invoking(c => c.GetAllHaplotypeFrequencies(setId)).Should().ThrowAsync<InvalidOperationException>();
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
