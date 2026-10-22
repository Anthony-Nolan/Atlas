using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.SupportMessages;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NUnit.Framework;
using HaplotypeFrequencySet = Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet.HaplotypeFrequencySet;
using HfSetHaplotypeNames = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.LociInfo<string>;

namespace Atlas.MatchPrediction.Test.Services.HaplotypeFrequencies;

[TestFixture]
internal class HaplotypeFrequencyServiceTests
{
    private IFrequencySetImporter frequencySetImporter;
    private INotificationSender notificationSender;
    private IMatchPredictionLogger<MatchProbabilityLoggingContext> logger;
    private IHaplotypeFrequencyCache haplotypeFrequencyCache;
    private FakeTimeProvider timeProvider;

    private Fixture fixture;

    private HaplotypeFrequencyService sut;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        frequencySetImporter = Substitute.For<IFrequencySetImporter>();
        notificationSender = Substitute.For<INotificationSender>();
        logger = Substitute.For<IMatchPredictionLogger<MatchProbabilityLoggingContext>>();
        haplotypeFrequencyCache = Substitute.For<IHaplotypeFrequencyCache>();
        timeProvider = new FakeTimeProvider();

        sut = new HaplotypeFrequencyService(
            frequencySetImporter,
            notificationSender,
            logger,
            haplotypeFrequencyCache,
            timeProvider
        );
    }

    [Test]
    public async Task GetSingleHaplotypeFrequencySet_ReturnsActiveSetMatchingMetadata()
    {
        var registryCode = fixture.Create<string>();
        var ethnicityCode = fixture.Create<string>();

        var activeSet = fixture.Build<HaplotypeFrequencySet>()
            .With(x => x.RegistryCode, registryCode)
            .With(x => x.EthnicityCode, ethnicityCode)
            .Create();

        haplotypeFrequencyCache.GetActiveHaplotypeFrequencySets().Returns(
            new Dictionary<(string, string), HaplotypeFrequencySet>
            {
                { (registryCode, ethnicityCode), activeSet }
            });

        var metadata = fixture.Build<FrequencySetMetadata>()
            .With(x => x.RegistryCode, registryCode)
            .With(x => x.EthnicityCode, ethnicityCode)
            .Create();

        var result = await sut.GetSingleHaplotypeFrequencySet(metadata);

        result.Id.Should().Be(activeSet.Id);
    }

    [Test]
    public async Task ImportFrequencySet_SuccessfulImport_RefreshesTheActiveSetsBeforeTheSuccessNotification()
    {
        frequencySetImporter.Import(null, null).ReturnsForAnyArgs(Task.CompletedTask);
        haplotypeFrequencyCache.RefreshActiveHaplotypeFrequencySets().Returns(true);

        await CompleteWhileTimePasses(sut.ImportFrequencySet(NewFile(), fixture.Create<FrequencySetImportBehaviour>()));

        Received.InOrder(() =>
        {
            haplotypeFrequencyCache.RefreshActiveHaplotypeFrequencySets();
            notificationSender.SendNotification(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
            haplotypeFrequencyCache.RefreshActiveHaplotypeFrequencySets();
        });
    }

    [Test]
    public async Task ImportFrequencySet_SuccessfulImport_RefreshesTheActiveSetsAgainAfterTheDelay()
    {
        frequencySetImporter.Import(null, null).ReturnsForAnyArgs(Task.CompletedTask);
        haplotypeFrequencyCache.RefreshActiveHaplotypeFrequencySets().Returns(true);

        var import = sut.ImportFrequencySet(NewFile(), fixture.Create<FrequencySetImportBehaviour>());
        timeProvider.Advance(HaplotypeFrequencyService.ActiveSetCacheRewriteDelay - TimeSpan.FromSeconds(1));
        await haplotypeFrequencyCache.Received(1).RefreshActiveHaplotypeFrequencySets();
        timeProvider.Advance(TimeSpan.FromSeconds(1));
        await import;

        await haplotypeFrequencyCache.Received(2).RefreshActiveHaplotypeFrequencySets();
    }

    [Test]
    public async Task ImportFrequencySet_WhenTheDistributedCacheNeverStoresTheSets_TriesFourMoreTimes_LogsEachTry_AndSendsAnAlert()
    {
        frequencySetImporter.Import(null, null).ReturnsForAnyArgs(Task.CompletedTask);
        haplotypeFrequencyCache.RefreshActiveHaplotypeFrequencySets().Returns(false);

        await CompleteWhileTimePasses(sut.ImportFrequencySet(NewFile(), fixture.Create<FrequencySetImportBehaviour>()));

        const int tries = 1 + HaplotypeFrequencyService.MaxActiveSetCacheRewriteAttempts;
        await haplotypeFrequencyCache.Received(tries).RefreshActiveHaplotypeFrequencySets();
        logger.Received(tries).SendTrace(
            Arg.Is<string>(message => message.Contains("did not store the active haplotype frequency sets")),
            LogLevel.Warn,
            Arg.Any<Dictionary<string, string>>());
        await notificationSender.Received(1).SendAlert(
            Arg.Is<string>(summary => summary.Contains("RefreshActiveHaplotypeFrequencySetCache")),
            Arg.Any<string>(),
            Priority.Medium,
            Arg.Any<string>());
    }

    [Test]
    public async Task ImportFrequencySet_WhenTheRefreshThrows_StillSucceeds_WithNoAlert()
    {
        frequencySetImporter.Import(null, null).ReturnsForAnyArgs(Task.CompletedTask);
        haplotypeFrequencyCache.RefreshActiveHaplotypeFrequencySets()
            .Returns(Task.FromException<bool>(new InvalidOperationException(fixture.Create<string>())), Task.FromResult(true));

        var import = () => CompleteWhileTimePasses(sut.ImportFrequencySet(NewFile(), fixture.Create<FrequencySetImportBehaviour>()));

        await import.Should().NotThrowAsync();
        await notificationSender.ReceivedWithAnyArgs(1).SendNotification(default, default, default);
        await notificationSender.DidNotReceiveWithAnyArgs().SendAlert(default, default, default, default);
    }

    [Test]
    public async Task RefreshActiveHaplotypeFrequencySetCache_WhenTheDistributedCacheStoresTheSets_ReturnsTrue()
    {
        haplotypeFrequencyCache.RefreshActiveHaplotypeFrequencySets().Returns(true);

        var refresh = sut.RefreshActiveHaplotypeFrequencySetCache();
        await CompleteWhileTimePasses(refresh);

        (await refresh).Should().BeTrue();
    }

    [Test]
    public async Task RefreshActiveHaplotypeFrequencySetCache_WhenTheDistributedCacheNeverStoresTheSets_ReturnsFalse_WithNoAlert()
    {
        haplotypeFrequencyCache.RefreshActiveHaplotypeFrequencySets().Returns(false);

        var refresh = sut.RefreshActiveHaplotypeFrequencySetCache();
        await CompleteWhileTimePasses(refresh);

        (await refresh).Should().BeFalse();
        // The caller of the endpoint gets the result. An alert is for an import, which nobody waits for.
        await notificationSender.DidNotReceiveWithAnyArgs().SendAlert(default, default, default, default);
    }

    // The typed catch blocks send a Medium-priority alert and swallow; an unexpected (generic) exception must instead
    // raise a High-priority failure alert AND propagate, so the import is retried / surfaced rather than lost.
    [Test]
    public async Task ImportFrequencySet_WhenImportThrowsUnexpectedException_SendsHighPriorityAlertAndRethrows()
    {
        var exception = new InvalidOperationException(fixture.Create<string>());
        frequencySetImporter.Import(null, null).ReturnsForAnyArgs(Task.FromException(exception));

        var file = fixture.Build<FrequencySetFile>()
            .Without(f => f.Contents)
            .Create();

        var import = () => sut.ImportFrequencySet(file, fixture.Create<FrequencySetImportBehaviour>());

        (await import.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(exception);
        await notificationSender.Received(1).SendAlert(Arg.Any<string>(), Arg.Any<string>(), Priority.High, Arg.Any<string>());
        await haplotypeFrequencyCache.DidNotReceive().RefreshActiveHaplotypeFrequencySets();
    }

    [Test]
    public async Task GetSingleHaplotypeFrequencySet_WhenNoSetMatchesAndNoGlobalSetExists_Throws()
    {
        haplotypeFrequencyCache.GetActiveHaplotypeFrequencySets()
            .Returns(new Dictionary<(string, string), HaplotypeFrequencySet>());

        var getSet = () => sut.GetSingleHaplotypeFrequencySet(fixture.Create<FrequencySetMetadata>());

        await getSet.Should().ThrowAsync<Exception>().WithMessage("No Global Haplotype frequency set was found");
    }

    [Test]
    public async Task GetFrequencyForHla_WhenHaplotypeIsPresentInSet_ReturnsItsFrequency()
    {
        const int setId = 1;
        haplotypeFrequencyCache.GetAllHaplotypeFrequencies(setId).Returns(BuildEntry(("a", "b", "c", "q", "r", 0.3m)));
        var hla = new HfSetHaplotypeNames(valueA: "a", valueB: "b", valueC: "c", valueDqb1: "q", valueDrb1: "r");

        var result = await sut.GetFrequencyForHla(setId, hla, new HashSet<Locus>());

        result.Should().Be(0.3m);
        haplotypeFrequencyCache.DidNotReceive()
            .GetConsolidatedFrequency(Arg.Any<FrequencySetCacheEntry>(), Arg.Any<HfSetHaplotypeNames>(), Arg.Any<ISet<Locus>>());
    }

    [Test]
    public async Task GetFrequencyForHla_WhenAlleleAbsentFromSet_AndNoExcludedLoci_ReturnsZeroWithoutConsolidating()
    {
        const int setId = 1;
        haplotypeFrequencyCache.GetAllHaplotypeFrequencies(setId).Returns(BuildEntry(("a", "b", "c", "q", "r", 0.3m)));
        // "zzz" at A never appears in the set, so the interner cannot resolve the haplotype at all.
        var hla = new HfSetHaplotypeNames(valueA: "zzz", valueB: "b", valueC: "c", valueDqb1: "q", valueDrb1: "r");

        var result = await sut.GetFrequencyForHla(setId, hla, new HashSet<Locus>());

        result.Should().Be(0);
        haplotypeFrequencyCache.DidNotReceive()
            .GetConsolidatedFrequency(Arg.Any<FrequencySetCacheEntry>(), Arg.Any<HfSetHaplotypeNames>(), Arg.Any<ISet<Locus>>());
    }

    [Test]
    public async Task GetFrequencyForHla_WhenAlleleAbsentFromSet_AndExcludedLoci_ReturnsConsolidatedFrequency()
    {
        const int setId = 1;
        haplotypeFrequencyCache.GetAllHaplotypeFrequencies(setId).Returns(BuildEntry(("a", "b", "c", "q", "r", 0.3m)));
        haplotypeFrequencyCache.GetConsolidatedFrequency(Arg.Any<FrequencySetCacheEntry>(), Arg.Any<HfSetHaplotypeNames>(), Arg.Any<ISet<Locus>>()).Returns(0.99m);
        var hla = new HfSetHaplotypeNames(valueA: "zzz", valueB: "b", valueC: "c", valueDqb1: "q", valueDrb1: "r");

        var result = await sut.GetFrequencyForHla(setId, hla, new HashSet<Locus> { Locus.C });

        result.Should().Be(0.99m);
        haplotypeFrequencyCache.Received(1).GetConsolidatedFrequency(Arg.Any<FrequencySetCacheEntry>(), hla, Arg.Any<ISet<Locus>>());
    }

    // Regression guard: the interner resolves each allele independently, so a cross-combination of alleles that all
    // exist individually (but was never imported as a haplotype) resolves to a key that is NOT in the dictionary.
    // Indexing into the dictionary on a successful resolve threw KeyNotFoundException for such haplotypes; we must
    // probe it and fall through to the unrepresented handling instead.
    [Test]
    public async Task GetFrequencyForHla_WhenAllelesResolveButCombinationAbsent_AndNoExcludedLoci_ReturnsZeroWithoutThrowing()
    {
        const int setId = 1;
        haplotypeFrequencyCache.GetAllHaplotypeFrequencies(setId).Returns(BuildEntry(
            ("a1", "b1", "c1", "q1", "r1", 0.1m),
            ("a2", "b2", "c2", "q2", "r2", 0.2m)));
        // Every allele below exists in the set, but this exact haplotype does not.
        var hla = new HfSetHaplotypeNames(valueA: "a1", valueB: "b2", valueC: "c1", valueDqb1: "q1", valueDrb1: "r1");

        var result = await sut.GetFrequencyForHla(setId, hla, new HashSet<Locus>());

        result.Should().Be(0);
        haplotypeFrequencyCache.DidNotReceive()
            .GetConsolidatedFrequency(Arg.Any<FrequencySetCacheEntry>(), Arg.Any<HfSetHaplotypeNames>(), Arg.Any<ISet<Locus>>());
    }

    [Test]
    public async Task GetFrequencyForHla_WhenAllelesResolveButCombinationAbsent_AndExcludedLoci_ReturnsConsolidatedFrequency()
    {
        const int setId = 1;
        haplotypeFrequencyCache.GetAllHaplotypeFrequencies(setId).Returns(BuildEntry(
            ("a1", "b1", "c1", "q1", "r1", 0.1m),
            ("a2", "b2", "c2", "q2", "r2", 0.2m)));
        haplotypeFrequencyCache.GetConsolidatedFrequency(Arg.Any<FrequencySetCacheEntry>(), Arg.Any<HfSetHaplotypeNames>(), Arg.Any<ISet<Locus>>()).Returns(0.55m);
        var hla = new HfSetHaplotypeNames(valueA: "a1", valueB: "b2", valueC: "c1", valueDqb1: "q1", valueDrb1: "r1");

        var result = await sut.GetFrequencyForHla(setId, hla, new HashSet<Locus> { Locus.C });

        result.Should().Be(0.55m);
        haplotypeFrequencyCache.Received(1).GetConsolidatedFrequency(Arg.Any<FrequencySetCacheEntry>(), hla, Arg.Any<ISet<Locus>>());
    }

    [Test]
    public async Task GetFrequenciesForHla_GivesEachHaplotypeTheAnswerOfGetFrequencyForHla_InOrder()
    {
        const int setId = 1;
        var entry = BuildEntry(
            ("a1", "b1", "c1", "q1", "r1", 0.1m),
            ("a2", "b2", "c2", "q2", "r2", 0.2m));
        haplotypeFrequencyCache.GetAllHaplotypeFrequencies(setId).Returns(entry);
        haplotypeFrequencyCache.GetConsolidatedFrequency(entry, Arg.Any<HfSetHaplotypeNames>(), Arg.Any<ISet<Locus>>()).Returns(0.55m);
        var hlas = new List<HfSetHaplotypeNames>
        {
            new(valueA: "a2", valueB: "b2", valueC: "c2", valueDqb1: "q2", valueDrb1: "r2"),
            // Unknown allele, and a cross-combination of known alleles: both unrepresented.
            new(valueA: "zzz", valueB: "b1", valueC: "c1", valueDqb1: "q1", valueDrb1: "r1"),
            new(valueA: "a1", valueB: "b2", valueC: "c1", valueDqb1: "q1", valueDrb1: "r1"),
            new(valueA: "a1", valueB: "b1", valueC: "c1", valueDqb1: "q1", valueDrb1: "r1"),
        };

        foreach (var excludedLoci in new[] { new HashSet<Locus>(), new HashSet<Locus> { Locus.C } })
        {
            var batch = await sut.GetFrequenciesForHla(setId, hlas, excludedLoci);

            var oneByOne = new List<decimal>();
            foreach (var hla in hlas)
            {
                oneByOne.Add(await sut.GetFrequencyForHla(setId, hla, excludedLoci));
            }

            batch.Should().Equal(oneByOne);
        }
    }

    [Test]
    public async Task GetFrequenciesForHla_FetchesTheSetOnce_AndConsolidatesFromThatEntry()
    {
        const int setId = 1;
        var entry = BuildEntry(("a", "b", "c", "q", "r", 0.3m));
        haplotypeFrequencyCache.GetAllHaplotypeFrequencies(setId).Returns(entry);
        haplotypeFrequencyCache.GetConsolidatedFrequency(entry, Arg.Any<HfSetHaplotypeNames>(), Arg.Any<ISet<Locus>>()).Returns(0.99m);
        var hlas = Enumerable.Range(0, 50)
            .Select(i => new HfSetHaplotypeNames(valueA: i == 0 ? "a" : $"unknown-{i}", valueB: "b", valueC: "c", valueDqb1: "q", valueDrb1: "r"))
            .ToList();

        var result = await sut.GetFrequenciesForHla(setId, hlas, new HashSet<Locus> { Locus.C });

        result.Should().HaveCount(50);
        result[0].Should().Be(0.3m);
        result.Skip(1).Should().AllSatisfy(f => f.Should().Be(0.99m));
        await haplotypeFrequencyCache.Received(1).GetAllHaplotypeFrequencies(setId);
        haplotypeFrequencyCache.Received(49).GetConsolidatedFrequency(entry, Arg.Any<HfSetHaplotypeNames>(), Arg.Any<ISet<Locus>>());
        await haplotypeFrequencyCache.DidNotReceive()
            .GetConsolidatedFrequency(Arg.Any<int>(), Arg.Any<HfSetHaplotypeNames>(), Arg.Any<ISet<Locus>>());
    }

    [Test]
    public async Task GetFrequenciesForHla_WithNoHaplotypes_ReturnsNoFrequencies()
    {
        const int setId = 1;
        haplotypeFrequencyCache.GetAllHaplotypeFrequencies(setId).Returns(BuildEntry(("a", "b", "c", "q", "r", 0.3m)));

        var result = await sut.GetFrequenciesForHla(setId, new List<HfSetHaplotypeNames>(), new HashSet<Locus>());

        result.Should().BeEmpty();
    }

    private FrequencySetFile NewFile() => fixture.Build<FrequencySetFile>().Without(f => f.Contents).Create();

    /// <summary>
    /// Moves the fake clock on, one rewrite delay at a time, until <paramref name="task"/> ends: each new write of the active
    /// sets waits for that delay first.
    /// </summary>
    private async Task CompleteWhileTimePasses(Task task)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!task.IsCompleted && DateTime.UtcNow < deadline)
        {
            timeProvider.Advance(HaplotypeFrequencyService.ActiveSetCacheRewriteDelay);
            await Task.Delay(10);
        }

        if (!task.IsCompleted)
        {
            throw new TimeoutException("The task did not end while the fake clock moved on.");
        }

        await task;
    }

    private static FrequencySetCacheEntry BuildEntry(
        params (string A, string B, string C, string Dqb1, string Drb1, decimal Frequency)[] haplotypes)
    {
        var interner = new HaplotypeInterner();
        var dictionary = new Dictionary<HaplotypeKey, HaplotypeFrequencyValue>();
        foreach (var haplotype in haplotypes)
        {
            var key = interner.Intern(haplotype.A, haplotype.B, haplotype.C, haplotype.Dqb1, haplotype.Drb1);
            dictionary[key] = new HaplotypeFrequencyValue(haplotype.Frequency, default);
        }

        return new FrequencySetCacheEntry
        {
            SetFrequencies = dictionary.ToFrozenDictionary(),
            Interner = interner
        };
    }
}
