using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Atlas.Client.Models.SupportMessages;
using Atlas.Common.Notifications;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import;
using AutoFixture;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;
using HaplotypeFrequencySet = Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet.HaplotypeFrequencySet;
using HaplotypeHla = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.LociInfo<string>;

namespace Atlas.MatchPrediction.Test.Services.HaplotypeFrequencies;

[TestFixture]
internal class HaplotypeFrequencyServiceTests
{
    private IFrequencySetImporter frequencySetImporter;
    private INotificationSender notificationSender;
    private IMatchPredictionLogger<MatchProbabilityLoggingContext> logger;
    private IHaplotypeFrequencyCache haplotypeFrequencyCache;

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

        sut = new HaplotypeFrequencyService(
            frequencySetImporter,
            notificationSender,
            logger,
            haplotypeFrequencyCache
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
    public async Task ImportFrequencySet_SuccessfulImport_InvalidatesActiveSetCache()
    {
        frequencySetImporter.Import(null, null).ReturnsForAnyArgs(Task.CompletedTask);

        var file = fixture.Build<FrequencySetFile>()
            .Without(f => f.Contents)
            .Create();

        await sut.ImportFrequencySet(file, fixture.Create<FrequencySetImportBehaviour>());

        haplotypeFrequencyCache.Received(1).RemoveActiveHaplotypeFrequencySets();
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
        haplotypeFrequencyCache.DidNotReceive().RemoveActiveHaplotypeFrequencySets();
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
        var hla = new HaplotypeHla(valueA: "a", valueB: "b", valueC: "c", valueDqb1: "q", valueDrb1: "r");

        var result = await sut.GetFrequencyForHla(setId, hla, new HashSet<Locus>());

        result.Should().Be(0.3m);
        await haplotypeFrequencyCache.DidNotReceive()
            .GetConsolidatedFrequency(Arg.Any<int>(), Arg.Any<HaplotypeHla>(), Arg.Any<ISet<Locus>>());
    }

    [Test]
    public async Task GetFrequencyForHla_WhenAlleleAbsentFromSet_AndNoExcludedLoci_ReturnsZeroWithoutConsolidating()
    {
        const int setId = 1;
        haplotypeFrequencyCache.GetAllHaplotypeFrequencies(setId).Returns(BuildEntry(("a", "b", "c", "q", "r", 0.3m)));
        // "zzz" at A never appears in the set, so the interner cannot resolve the haplotype at all.
        var hla = new HaplotypeHla(valueA: "zzz", valueB: "b", valueC: "c", valueDqb1: "q", valueDrb1: "r");

        var result = await sut.GetFrequencyForHla(setId, hla, new HashSet<Locus>());

        result.Should().Be(0);
        await haplotypeFrequencyCache.DidNotReceive()
            .GetConsolidatedFrequency(Arg.Any<int>(), Arg.Any<HaplotypeHla>(), Arg.Any<ISet<Locus>>());
    }

    [Test]
    public async Task GetFrequencyForHla_WhenAlleleAbsentFromSet_AndExcludedLoci_ReturnsConsolidatedFrequency()
    {
        const int setId = 1;
        haplotypeFrequencyCache.GetAllHaplotypeFrequencies(setId).Returns(BuildEntry(("a", "b", "c", "q", "r", 0.3m)));
        haplotypeFrequencyCache.GetConsolidatedFrequency(setId, Arg.Any<HaplotypeHla>(), Arg.Any<ISet<Locus>>()).Returns(0.99m);
        var hla = new HaplotypeHla(valueA: "zzz", valueB: "b", valueC: "c", valueDqb1: "q", valueDrb1: "r");

        var result = await sut.GetFrequencyForHla(setId, hla, new HashSet<Locus> { Locus.C });

        result.Should().Be(0.99m);
        await haplotypeFrequencyCache.Received(1).GetConsolidatedFrequency(setId, hla, Arg.Any<ISet<Locus>>());
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
        var hla = new HaplotypeHla(valueA: "a1", valueB: "b2", valueC: "c1", valueDqb1: "q1", valueDrb1: "r1");

        var result = await sut.GetFrequencyForHla(setId, hla, new HashSet<Locus>());

        result.Should().Be(0);
        await haplotypeFrequencyCache.DidNotReceive()
            .GetConsolidatedFrequency(Arg.Any<int>(), Arg.Any<HaplotypeHla>(), Arg.Any<ISet<Locus>>());
    }

    [Test]
    public async Task GetFrequencyForHla_WhenAllelesResolveButCombinationAbsent_AndExcludedLoci_ReturnsConsolidatedFrequency()
    {
        const int setId = 1;
        haplotypeFrequencyCache.GetAllHaplotypeFrequencies(setId).Returns(BuildEntry(
            ("a1", "b1", "c1", "q1", "r1", 0.1m),
            ("a2", "b2", "c2", "q2", "r2", 0.2m)));
        haplotypeFrequencyCache.GetConsolidatedFrequency(setId, Arg.Any<HaplotypeHla>(), Arg.Any<ISet<Locus>>()).Returns(0.55m);
        var hla = new HaplotypeHla(valueA: "a1", valueB: "b2", valueC: "c1", valueDqb1: "q1", valueDrb1: "r1");

        var result = await sut.GetFrequencyForHla(setId, hla, new HashSet<Locus> { Locus.C });

        result.Should().Be(0.55m);
        await haplotypeFrequencyCache.Received(1).GetConsolidatedFrequency(setId, hla, Arg.Any<ISet<Locus>>());
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
