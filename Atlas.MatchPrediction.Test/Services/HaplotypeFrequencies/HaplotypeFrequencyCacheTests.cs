using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using HaplotypeHla = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.LociInfo<string>;

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

        var cacheSettings = fixture.Build<HaplotypeFrequencySetCacheSettings>()
            .With(x => x.ActiveSetCacheExpiryMinutes, 5)
            .Create();

        sut = new HaplotypeFrequencyCache(
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
                Arg.Any<HaplotypeHla>(),
                Arg.Any<ISet<Locus>>())
            .Returns(expectedFrequency);

        var hla = new HaplotypeHla(valueA: "a", valueB: "b", valueC: "c", valueDqb1: "dqb1", valueDrb1: "drb1");

        var result = await sut.GetConsolidatedFrequency(setId, hla, new HashSet<Locus> { Locus.C });

        result.Should().Be(expectedFrequency);
    }

    [Test]
    public async Task GetConsolidatedFrequency_AfterWarmingCompletes_ReadsFromConsolidatedCollection()
    {
        const int setId = 5;
        const decimal expectedFrequency = 0.42m;
        var hla = new HaplotypeHla(valueA: "a", valueB: "b", valueC: "c", valueDqb1: "dqb1", valueDrb1: "drb1");
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
            Arg.Any<FrequencySetCacheEntry>(), Arg.Any<HaplotypeHla>(), Arg.Any<ISet<Locus>>());
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
