using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MatchPrediction.Services.Precompute;
using AwesomeAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.Precompute;

[TestFixture]
internal class DonorGenotypeSetSourceResolverTests
{
    private const int DataRefreshRecordId = 42;
    private static readonly IReadOnlyCollection<DonorInput> Donors = [new() { DonorIds = [1, 2] }];

    private PrecomputedGenotypeSetSettings settings;
    private IPrecomputedDonorGenotypeSetReader reader;
    private IDonorGenotypeSetSourceResolver resolver;

    [SetUp]
    public void SetUp()
    {
        settings = new PrecomputedGenotypeSetSettings();
        reader = Substitute.For<IPrecomputedDonorGenotypeSetReader>();
        reader.GetDonorGenotypeSets(default, default, default).ReturnsForAnyArgs(
            new PrecomputedDonorGenotypeSetLookup("ABCDrb1Dqb1", null, new Dictionary<int, PrecomputedDonorGenotypeSetRow>()));

        resolver = new DonorGenotypeSetSourceResolver(settings, reader, Substitute.For<IAtlasLogger>());
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

        await reader.DidNotReceiveWithAnyArgs().GetDonorGenotypeSets(default, default, default);
        context.BatchFallbackReason.Should().Be(DonorGenotypeSetSource.PrecomputeDisabled);
        context.CanStore.Should().BeFalse();
    }

    [Test]
    public async Task Resolve_WhenSwitchedOff_DoesNotReadAndDisablesTheBatch()
    {
        settings.Mode = PrecomputedGenotypeSetMode.DefaultPrecomputed;

        var context = await resolver.Resolve(Request(false), Donors);

        await reader.DidNotReceiveWithAnyArgs().GetDonorGenotypeSets(default, default, default);
        context.BatchFallbackReason.Should().Be(DonorGenotypeSetSource.PrecomputeDisabled);
        context.CanStore.Should().BeFalse();
    }

    [Test]
    public async Task Resolve_WhenSwitchedOn_ReadsTheBatchOnceWithItsDonorInputsLociAndRecord()
    {
        settings.Mode = PrecomputedGenotypeSetMode.DefaultLive;
        var request = Request(true);
        request.ExcludedLoci = [Locus.C];

        var context = await resolver.Resolve(request, Donors);

        await reader.Received(1).GetDonorGenotypeSets(
            Donors,
            Arg.Is<IReadOnlySet<Locus>>(loci => loci.SetEquals(new[] { Locus.A, Locus.B, Locus.Dqb1, Locus.Drb1 })),
            DataRefreshRecordId);
        context.BatchFallbackReason.Should().BeNull();
        context.AllowedLociKey.Should().Be("ABCDrb1Dqb1");
        context.CanStore.Should().BeTrue();
    }

    [Test]
    public async Task Resolve_PassesTheReadersBatchFallbackReasonOn()
    {
        settings.Mode = PrecomputedGenotypeSetMode.DefaultPrecomputed;
        reader.GetDonorGenotypeSets(default, default, default).ReturnsForAnyArgs(
            PrecomputedDonorGenotypeSetLookup.Unavailable(DonorGenotypeSetSource.ActiveDatabaseChanged, "ABDrb1"));

        var context = await resolver.Resolve(Request(null), Donors);

        context.BatchFallbackReason.Should().Be(DonorGenotypeSetSource.ActiveDatabaseChanged);
        context.CanStore.Should().BeFalse();
    }

    [Test]
    public async Task Resolve_WhenTheReadFails_ComputesTheBatchLiveRatherThanFailing()
    {
        settings.Mode = PrecomputedGenotypeSetMode.DefaultPrecomputed;
        reader.GetDonorGenotypeSets(default, default, default).ThrowsAsyncForAnyArgs(new Exception("sql is down"));

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
        await reader.ReceivedWithAnyArgs(1).GetDonorGenotypeSets(default, default, default);
    }

    private static IdentifiedMatchProbabilityRequest Request(bool? usePrecomputedGenotypeSets) => new()
    {
        SearchRequestId = "search-id",
        MatchingAlgorithmDataRefreshRecordId = DataRefreshRecordId,
        UsePrecomputedGenotypeSets = usePrecomputedGenotypeSets
    };
}
