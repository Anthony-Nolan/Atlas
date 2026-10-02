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
    private static readonly IReadOnlyCollection<int> DonorIds = [1, 2];

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

    [TestCase(null, false, false, UsePrecomputedGenotypeSetsSource.FeatureFlag)]
    [TestCase(null, true, true, UsePrecomputedGenotypeSetsSource.FeatureFlag)]
    [TestCase(true, false, true, UsePrecomputedGenotypeSetsSource.Request)]
    [TestCase(true, true, true, UsePrecomputedGenotypeSetsSource.Request)]
    [TestCase(false, false, false, UsePrecomputedGenotypeSetsSource.Request)]
    [TestCase(false, true, false, UsePrecomputedGenotypeSetsSource.Request)]
    public async Task Resolve_RequestOverridesTheKillSwitchInBothDirections(
        bool? requestValue,
        bool flag,
        bool expectedUse,
        UsePrecomputedGenotypeSetsSource expectedSource)
    {
        settings.UsePrecomputedGenotypeSets = flag;

        var context = await resolver.Resolve(Request(requestValue), DonorIds);

        context.UsePrecomputedGenotypeSets.Should().Be(expectedUse);
        context.UsePrecomputedGenotypeSetsSource.Should().Be(expectedSource);
    }

    [Test]
    public async Task Resolve_WhenSwitchedOff_DoesNotReadAndDisablesTheBatch()
    {
        settings.UsePrecomputedGenotypeSets = true;

        var context = await resolver.Resolve(Request(false), DonorIds);

        await reader.DidNotReceiveWithAnyArgs().GetDonorGenotypeSets(default, default, default);
        context.BatchFallbackReason.Should().Be(DonorGenotypeSetSource.PrecomputeDisabled);
        context.CanStore.Should().BeFalse();
    }

    [Test]
    public async Task Resolve_WhenSwitchedOn_ReadsTheBatchOnceWithTheSearchLociAndRecord()
    {
        var request = Request(true);
        request.ExcludedLoci = [Locus.C];

        var context = await resolver.Resolve(request, DonorIds);

        await reader.Received(1).GetDonorGenotypeSets(
            DonorIds,
            Arg.Is<IReadOnlySet<Locus>>(loci => loci.SetEquals(new[] { Locus.A, Locus.B, Locus.Dqb1, Locus.Drb1 })),
            DataRefreshRecordId);
        context.BatchFallbackReason.Should().BeNull();
        context.AllowedLociKey.Should().Be("ABCDrb1Dqb1");
        context.CanStore.Should().BeTrue();
    }

    [Test]
    public async Task Resolve_PassesTheReadersBatchFallbackReasonOn()
    {
        reader.GetDonorGenotypeSets(default, default, default).ReturnsForAnyArgs(
            PrecomputedDonorGenotypeSetLookup.Unavailable(DonorGenotypeSetSource.ActiveDatabaseChanged, "ABDrb1"));

        var context = await resolver.Resolve(Request(true), DonorIds);

        context.BatchFallbackReason.Should().Be(DonorGenotypeSetSource.ActiveDatabaseChanged);
        context.CanStore.Should().BeFalse();
    }

    [Test]
    public async Task Resolve_WhenTheReadFails_ComputesTheBatchLiveRatherThanFailing()
    {
        reader.GetDonorGenotypeSets(default, default, default).ThrowsAsyncForAnyArgs(new Exception("sql is down"));

        var context = await resolver.Resolve(Request(true), DonorIds);

        context.BatchFallbackReason.Should().Be(DonorGenotypeSetSource.ReadFailed);
        context.CanStore.Should().BeFalse();
    }

    /// <summary>
    /// The value is resolved for each batch, not when the search is submitted, so turning the kill-switch off reaches a
    /// search that is already running and leaves the override null.
    /// </summary>
    [Test]
    public async Task Resolve_WhenTheKillSwitchChangesBetweenBatchesOfOneSearch_EachBatchUsesTheCurrentValue()
    {
        var request = Request(null);

        settings.UsePrecomputedGenotypeSets = true;
        var firstBatch = await resolver.Resolve(request, DonorIds);

        settings.UsePrecomputedGenotypeSets = false;
        var secondBatch = await resolver.Resolve(request, DonorIds);

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
