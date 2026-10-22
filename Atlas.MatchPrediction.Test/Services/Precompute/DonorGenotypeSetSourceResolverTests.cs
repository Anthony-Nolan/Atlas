using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.Precompute;
using AutoFixture;
using AwesomeAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using HaplotypeFrequencySet = Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet.HaplotypeFrequencySet;

namespace Atlas.MatchPrediction.Test.Services.Precompute;

[TestFixture]
internal class DonorGenotypeSetSourceResolverTests
{
    private const int DataRefreshRecordId = 42;
    private static readonly IReadOnlyCollection<DonorInput> Donors = [new() { DonorIds = [1, 2] }];

    private Fixture fixture;
    private PrecomputedGenotypeSetSettings settings;
    private IPrecomputedDonorGenotypeSetReader reader;
    private IHaplotypeFrequencyLookupService frequencySetLookup;
    private IDonorGenotypeSetSourceResolver resolver;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        settings = new PrecomputedGenotypeSetSettings();
        reader = Substitute.For<IPrecomputedDonorGenotypeSetReader>();
        reader.GetDonorGenotypeSets(default, default, default, default).ReturnsForAnyArgs(
            new PrecomputedDonorGenotypeSetLookup("ABCDrb1Dqb1", null, new Dictionary<int, PrecomputedDonorGenotypeSetRow>()));
        frequencySetLookup = Substitute.For<IHaplotypeFrequencyLookupService>();
        frequencySetLookup.GetSingleHaplotypeFrequencySet(default).ReturnsForAnyArgs(fixture.Create<HaplotypeFrequencySet>());

        resolver = new DonorGenotypeSetSourceResolver(settings, reader, frequencySetLookup, Substitute.For<IAtlasLogger>());
    }

    [Test]
    public void Settings_ByDefault_ForceLive()
    {
        new PrecomputedGenotypeSetSettings().Mode.Should().Be(PrecomputedGenotypeSetMode.ForceLive);
    }

    [TestCase(PrecomputedGenotypeSetMode.ForceLive, null, false, UsePrecomputedGenotypeSetsSource.FeatureFlag)]
    [TestCase(PrecomputedGenotypeSetMode.ForceLive, true, false, UsePrecomputedGenotypeSetsSource.FeatureFlag)]
    [TestCase(PrecomputedGenotypeSetMode.ForceLive, false, false, UsePrecomputedGenotypeSetsSource.FeatureFlag)]
    [TestCase(PrecomputedGenotypeSetMode.DefaultLive, null, false, UsePrecomputedGenotypeSetsSource.FeatureFlag)]
    [TestCase(PrecomputedGenotypeSetMode.DefaultLive, true, true, UsePrecomputedGenotypeSetsSource.Request)]
    [TestCase(PrecomputedGenotypeSetMode.DefaultLive, false, false, UsePrecomputedGenotypeSetsSource.Request)]
    [TestCase(PrecomputedGenotypeSetMode.DefaultPrecomputed, null, true, UsePrecomputedGenotypeSetsSource.FeatureFlag)]
    [TestCase(PrecomputedGenotypeSetMode.DefaultPrecomputed, true, true, UsePrecomputedGenotypeSetsSource.Request)]
    [TestCase(PrecomputedGenotypeSetMode.DefaultPrecomputed, false, false, UsePrecomputedGenotypeSetsSource.Request)]
    public async Task Resolve_CombinesTheModeAndTheRequestOverride(
        PrecomputedGenotypeSetMode mode,
        bool? requestValue,
        bool expectedUse,
        UsePrecomputedGenotypeSetsSource expectedSource)
    {
        settings.Mode = mode;

        var context = await resolver.Resolve(Request(requestValue), Donors);

        context.UsePrecomputedGenotypeSets.Should().Be(expectedUse);
        context.UsePrecomputedGenotypeSetsSource.Should().Be(expectedSource);
        context.Mode.Should().Be(mode);
    }

    /// <summary>The hard off: a request that opts in still gets nothing read, nothing trusted and nothing stored.</summary>
    [Test]
    public async Task Resolve_InForceLive_IgnoresARequestThatOptsInAndDoesNotRead()
    {
        settings.Mode = PrecomputedGenotypeSetMode.ForceLive;

        var context = await resolver.Resolve(Request(true), Donors);

        await reader.DidNotReceiveWithAnyArgs().GetDonorGenotypeSets(default, default, default, default);
        context.BatchFallbackReason.Should().Be(DonorGenotypeSetSource.PrecomputeDisabled);
        context.CanStore.Should().BeFalse();
    }

    [Test]
    public async Task Resolve_WhenSwitchedOff_DoesNotReadAndDisablesTheBatch()
    {
        settings.Mode = PrecomputedGenotypeSetMode.DefaultPrecomputed;

        var context = await resolver.Resolve(Request(false), Donors);

        await reader.DidNotReceiveWithAnyArgs().GetDonorGenotypeSets(default, default, default, default);
        context.BatchFallbackReason.Should().Be(DonorGenotypeSetSource.PrecomputeDisabled);
        context.CanStore.Should().BeFalse();
        context.ReadTime.Should().BeNull();
    }

    [Test]
    public async Task Resolve_WhenRead_ReportsTheReadTimeRowsAndBytes()
    {
        settings.Mode = PrecomputedGenotypeSetMode.DefaultPrecomputed;
        reader.GetDonorGenotypeSets(default, default, default, default).ReturnsForAnyArgs(async _ =>
        {
            await Task.Delay(20);
            return new PrecomputedDonorGenotypeSetLookup(
                "ABCDrb1Dqb1",
                null,
                new Dictionary<int, PrecomputedDonorGenotypeSetRow>
                {
                    { 1, new PrecomputedDonorGenotypeSetRow(7, false, new byte[100]) },
                    { 2, new PrecomputedDonorGenotypeSetRow(7, true, null) },
                },
                new PrecomputedPatientGenotypeSetRow(false, new byte[30]));
        });

        var context = await resolver.Resolve(Request(null), Donors);

        context.ReadTime.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(15));
        context.ReadRowCount.Should().Be(2);
        context.ReadBytes.Should().Be(130);
    }

    [Test]
    public async Task Resolve_WhenIdsAndThePatientShareARow_CountsTheRowAndItsBytesOnce()
    {
        settings.Mode = PrecomputedGenotypeSetMode.DefaultPrecomputed;
        var payload = fixture.CreateMany<byte>(fixture.Create<int>() % 100 + 1).ToArray();
        var sharedRow = new PrecomputedDonorGenotypeSetRow(fixture.Create<int>(), false, payload);
        var donorIds = fixture.CreateMany<int>(3).ToList();
        reader.GetDonorGenotypeSets(default, default, default, default).ReturnsForAnyArgs(new PrecomputedDonorGenotypeSetLookup(
            "ABCDrb1Dqb1",
            null,
            donorIds.ToDictionary(donorId => donorId, _ => sharedRow),
            new PrecomputedPatientGenotypeSetRow(false, payload)));

        var context = await resolver.Resolve(Request(null), Donors);

        context.ReadRowCount.Should().Be(1);
        context.ReadBytes.Should().Be(payload.Length);
    }

    [Test]
    public async Task Resolve_WhenSwitchedOn_ReadsTheBatchOnceWithItsDonorInputsLociAndRecord()
    {
        settings.Mode = PrecomputedGenotypeSetMode.DefaultLive;
        var request = Request(true);
        request.ExcludedLoci = [Locus.C];

        var context = await resolver.Resolve(request, Donors);

        await reader.Received(1).GetDonorGenotypeSets(
            Arg.Is<IReadOnlyCollection<DonorGenotypeSetToRead>>(donors => donors.Select(donor => donor.Donor).SequenceEqual(Donors)),
            Arg.Is<IReadOnlySet<Locus>>(loci => loci.SetEquals(new[] { Locus.A, Locus.B, Locus.Dqb1, Locus.Drb1 })),
            DataRefreshRecordId,
            null);
        context.BatchFallbackReason.Should().BeNull();
        context.AllowedLociKey.Should().Be("ABCDrb1Dqb1");
        context.CanStore.Should().BeTrue();
    }

    /// <summary>The key of a stored set holds its frequency set, so the read needs the set that the live path would use.</summary>
    [Test]
    public async Task Resolve_WhenSwitchedOn_ReadsEachDonorInputWithTheFrequencySetOfItsMetadata()
    {
        settings.Mode = PrecomputedGenotypeSetMode.DefaultPrecomputed;
        var firstMetadata = fixture.Create<FrequencySetMetadata>();
        var secondMetadata = fixture.Create<FrequencySetMetadata>();
        var firstSet = fixture.Create<HaplotypeFrequencySet>();
        var secondSet = fixture.Create<HaplotypeFrequencySet>();
        frequencySetLookup.GetSingleHaplotypeFrequencySet(firstMetadata).Returns(firstSet);
        frequencySetLookup.GetSingleHaplotypeFrequencySet(secondMetadata).Returns(secondSet);
        var first = new DonorInput { DonorIds = [fixture.Create<int>()], DonorFrequencySetMetadata = firstMetadata };
        var second = new DonorInput { DonorIds = [fixture.Create<int>()], DonorFrequencySetMetadata = secondMetadata };

        await resolver.Resolve(Request(null), [first, second]);

        await reader.Received(1).GetDonorGenotypeSets(
            Arg.Is<IReadOnlyCollection<DonorGenotypeSetToRead>>(donors => donors.SequenceEqual(new[]
            {
                new DonorGenotypeSetToRead(first, firstSet.Id),
                new DonorGenotypeSetToRead(second, secondSet.Id)
            })),
            Arg.Any<IReadOnlySet<Locus>>(),
            Arg.Any<int?>(),
            Arg.Any<PatientGenotypeSetKey>());
    }

    /// <summary>A batch's inputs are sorted by registry and ethnicity, so most of them share the lookup of the input before.</summary>
    [Test]
    public async Task Resolve_LooksUpTheFrequencySetOnceForEachRegistryAndEthnicity()
    {
        settings.Mode = PrecomputedGenotypeSetMode.DefaultPrecomputed;
        var metadata = fixture.Create<FrequencySetMetadata>();
        var sameCodes = new FrequencySetMetadata { RegistryCode = metadata.RegistryCode, EthnicityCode = metadata.EthnicityCode };
        var otherCodes = fixture.Create<FrequencySetMetadata>();
        var donors = new[] { metadata, sameCodes, otherCodes }
            .Select(codes => new DonorInput { DonorIds = [fixture.Create<int>()], DonorFrequencySetMetadata = codes })
            .ToList();

        await resolver.Resolve(Request(null), donors);

        await frequencySetLookup.ReceivedWithAnyArgs(2).GetSingleHaplotypeFrequencySet(default);
    }

    /// <summary>As the live path does: an input with no metadata gets the set of empty metadata, usually the global set.</summary>
    [Test]
    public async Task Resolve_ForADonorInputWithNoFrequencySetMetadata_LooksUpTheSetOfEmptyMetadata()
    {
        settings.Mode = PrecomputedGenotypeSetMode.DefaultPrecomputed;

        await resolver.Resolve(Request(null), [new DonorInput { DonorIds = [fixture.Create<int>()] }]);

        await frequencySetLookup.Received(1).GetSingleHaplotypeFrequencySet(
            Arg.Is<FrequencySetMetadata>(metadata => metadata.RegistryCode == null && metadata.EthnicityCode == null));
    }

    [Test]
    public async Task Resolve_WhenTheFrequencySetLookupFails_ComputesTheBatchLiveRatherThanFailing()
    {
        settings.Mode = PrecomputedGenotypeSetMode.DefaultPrecomputed;
        frequencySetLookup.GetSingleHaplotypeFrequencySet(default).ThrowsAsyncForAnyArgs(new Exception(fixture.Create<string>()));

        var context = await resolver.Resolve(Request(null), Donors);

        context.BatchFallbackReason.Should().Be(DonorGenotypeSetSource.ReadFailed);
        context.CanStore.Should().BeFalse();
        await reader.DidNotReceiveWithAnyArgs().GetDonorGenotypeSets(default, default, default, default);
    }

    [Test]
    public async Task Resolve_WhenSwitchedOff_DoesNotLookUpFrequencySets()
    {
        settings.Mode = PrecomputedGenotypeSetMode.ForceLive;

        await resolver.Resolve(Request(true), Donors);

        await frequencySetLookup.DidNotReceiveWithAnyArgs().GetSingleHaplotypeFrequencySet(default);
    }

    [Test]
    public async Task Resolve_PassesTheReadersBatchFallbackReasonOn()
    {
        settings.Mode = PrecomputedGenotypeSetMode.DefaultPrecomputed;
        reader.GetDonorGenotypeSets(default, default, default, default).ReturnsForAnyArgs(
            PrecomputedDonorGenotypeSetLookup.Unavailable(DonorGenotypeSetSource.ActiveDatabaseChanged, "ABDrb1"));

        var context = await resolver.Resolve(Request(null), Donors);

        context.BatchFallbackReason.Should().Be(DonorGenotypeSetSource.ActiveDatabaseChanged);
        context.CanStore.Should().BeFalse();
    }

    [Test]
    public async Task Resolve_WhenTheReadFails_ComputesTheBatchLiveRatherThanFailing()
    {
        settings.Mode = PrecomputedGenotypeSetMode.DefaultPrecomputed;
        reader.GetDonorGenotypeSets(default, default, default, default).ThrowsAsyncForAnyArgs(new Exception("sql is down"));

        var context = await resolver.Resolve(Request(null), Donors);

        context.BatchFallbackReason.Should().Be(DonorGenotypeSetSource.ReadFailed);
        context.CanStore.Should().BeFalse();
    }

    /// <summary>
    /// The value is resolved for each batch, not when the search is submitted, so switching to ForceLive reaches a
    /// search that is already running - even one that opted in.
    /// </summary>
    [Test]
    public async Task Resolve_WhenTheModeChangesBetweenBatchesOfOneSearch_EachBatchUsesTheCurrentMode()
    {
        var request = Request(true);

        settings.Mode = PrecomputedGenotypeSetMode.DefaultLive;
        var firstBatch = await resolver.Resolve(request, Donors);

        settings.Mode = PrecomputedGenotypeSetMode.ForceLive;
        var secondBatch = await resolver.Resolve(request, Donors);

        firstBatch.UsePrecomputedGenotypeSets.Should().BeTrue();
        secondBatch.UsePrecomputedGenotypeSets.Should().BeFalse();
        await reader.ReceivedWithAnyArgs(1).GetDonorGenotypeSets(default, default, default, default);
    }

    [Test]
    public async Task Resolve_WhenTheBatchHasAPatientKey_ReadsThePatientRowWithTheDonorsAndKeepsTheKey()
    {
        settings.Mode = PrecomputedGenotypeSetMode.DefaultPrecomputed;
        var key = new PatientGenotypeSetKey("typing-key", 7, "ABCDrb1Dqb1");
        var request = Request(null);
        request.PatientGenotypeSetKey = key;

        var context = await resolver.Resolve(request, Donors);

        await reader.Received(1).GetDonorGenotypeSets(
            Arg.Any<IReadOnlyCollection<DonorGenotypeSetToRead>>(), Arg.Any<IReadOnlySet<Locus>>(), DataRefreshRecordId, key);
        context.PatientGenotypeSetKey.Should().Be(key);
    }

    [Test]
    public async Task Resolve_WhenSwitchedOff_KeepsThePatientKeyButReadsNothing()
    {
        settings.Mode = PrecomputedGenotypeSetMode.ForceLive;
        var request = Request(true);
        request.PatientGenotypeSetKey = new PatientGenotypeSetKey("typing-key", 7, "ABCDrb1Dqb1");

        var context = await resolver.Resolve(request, Donors);

        await reader.DidNotReceiveWithAnyArgs().GetDonorGenotypeSets(default, default, default, default);
        context.PatientGenotypeSetKey.Should().Be(request.PatientGenotypeSetKey);
        context.TryGetPrecomputedPatientGenotypeSet(7, out _).Should().Be(PatientGenotypeSetSource.PrecomputeDisabled);
    }

    private static IdentifiedMatchProbabilityRequest Request(bool? usePrecomputedGenotypeSets) => new()
    {
        SearchRequestId = "search-id",
        MatchingAlgorithmDataRefreshRecordId = DataRefreshRecordId,
        UsePrecomputedGenotypeSets = usePrecomputedGenotypeSets
    };
}
