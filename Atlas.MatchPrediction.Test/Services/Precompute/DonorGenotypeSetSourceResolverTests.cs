using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
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

    private IPrecomputedDonorGenotypeSetReader reader;
    private IGenotypeSetReadRecorder readRecorder;
    private IDonorGenotypeSetSourceResolver resolver;

    [SetUp]
    public void SetUp()
    {
        reader = Substitute.For<IPrecomputedDonorGenotypeSetReader>();
        reader.GetDonorGenotypeSets(default, default, default, default).ReturnsForAnyArgs(
            new PrecomputedDonorGenotypeSetLookup("ABCDrb1Dqb1", null, new Dictionary<int, PrecomputedDonorGenotypeSetRow>()));

        readRecorder = Substitute.For<IGenotypeSetReadRecorder>();

        resolver = new DonorGenotypeSetSourceResolver(reader, readRecorder, Substitute.For<IAtlasLogger>());
    }

    [TestCase(null, false)]
    [TestCase(false, false)]
    [TestCase(true, true)]
    public async Task Resolve_UsesTheValueTheAtlasPublicApiSetOnTheRequest(bool? requestValue, bool expectedUse)
    {
        var context = await resolver.Resolve(Request(requestValue), Donors);

        context.UsePrecomputedGenotypeSets.Should().Be(expectedUse);
    }

    /// <summary>An empty value means the search did not pass through the Atlas Public API, so it is computed live.</summary>
    [Test]
    public async Task Resolve_WhenTheRequestValueIsEmpty_DoesNotReadAndDisablesTheBatch()
    {
        var context = await resolver.Resolve(Request(null), Donors);

        await reader.DidNotReceiveWithAnyArgs().GetDonorGenotypeSets(default, default, default, default);
        context.BatchFallbackReason.Should().Be(DonorGenotypeSetSource.PrecomputeDisabled);
        context.CanStore.Should().BeFalse();
    }

    [Test]
    public async Task Resolve_WhenSwitchedOff_DoesNotReadAndDisablesTheBatch()
    {
        var context = await resolver.Resolve(Request(false), Donors);

        await reader.DidNotReceiveWithAnyArgs().GetDonorGenotypeSets(default, default, default, default);
        context.BatchFallbackReason.Should().Be(DonorGenotypeSetSource.PrecomputeDisabled);
        context.CanStore.Should().BeFalse();
    }

    [Test]
    public async Task Resolve_WhenSwitchedOn_ReadsTheBatchOnceWithItsDonorInputsLociAndRecord()
    {
        var request = Request(true);
        request.ExcludedLoci = [Locus.C];

        var context = await resolver.Resolve(request, Donors);

        await reader.Received(1).GetDonorGenotypeSets(
            Donors,
            Arg.Is<IReadOnlySet<Locus>>(loci => loci.SetEquals(new[] { Locus.A, Locus.B, Locus.Dqb1, Locus.Drb1 })),
            DataRefreshRecordId,
            null);
        context.BatchFallbackReason.Should().BeNull();
        context.AllowedLociKey.Should().Be("ABCDrb1Dqb1");
        context.CanStore.Should().BeTrue();
    }

    [Test]
    public async Task Resolve_PassesTheReadersBatchFallbackReasonOn()
    {
        reader.GetDonorGenotypeSets(default, default, default, default).ReturnsForAnyArgs(
            PrecomputedDonorGenotypeSetLookup.Unavailable(DonorGenotypeSetSource.ActiveDatabaseChanged, "ABDrb1"));

        var context = await resolver.Resolve(Request(true), Donors);

        context.BatchFallbackReason.Should().Be(DonorGenotypeSetSource.ActiveDatabaseChanged);
        context.CanStore.Should().BeFalse();
    }

    [Test]
    public async Task Resolve_WhenTheReadFails_ComputesTheBatchLiveRatherThanFailing()
    {
        reader.GetDonorGenotypeSets(default, default, default, default).ThrowsAsyncForAnyArgs(new Exception("sql is down"));

        var context = await resolver.Resolve(Request(true), Donors);

        context.BatchFallbackReason.Should().Be(DonorGenotypeSetSource.ReadFailed);
        context.CanStore.Should().BeFalse();
    }

    [Test]
    public async Task Resolve_WhenTheBatchHasAPatientKey_ReadsThePatientRowWithTheDonorsAndKeepsTheKey()
    {
        var key = new PatientGenotypeSetKey("typing-key", 7, "ABCDrb1Dqb1");
        var request = Request(true);
        request.PatientGenotypeSetKey = key;

        var context = await resolver.Resolve(request, Donors);

        await reader.Received(1).GetDonorGenotypeSets(Donors, Arg.Any<IReadOnlySet<Locus>>(), DataRefreshRecordId, key);
        context.PatientGenotypeSetKey.Should().Be(key);
    }

    [Test]
    public async Task Resolve_WhenSwitchedOff_KeepsThePatientKeyButReadsNothing()
    {
        var request = Request(false);
        request.PatientGenotypeSetKey = new PatientGenotypeSetKey("typing-key", 7, "ABCDrb1Dqb1");

        var context = await resolver.Resolve(request, Donors);

        await reader.DidNotReceiveWithAnyArgs().GetDonorGenotypeSets(default, default, default, default);
        context.PatientGenotypeSetKey.Should().Be(request.PatientGenotypeSetKey);
        context.TryGetPrecomputedPatientGenotypeSet(7, out _).Should().Be(PatientGenotypeSetSource.PrecomputeDisabled);
    }

    /// <summary>The ATL-347 measurement needs live batches too, so the batch is recorded on both paths (ATL-571).</summary>
    [TestCase(true)]
    [TestCase(false)]
    [TestCase(null)]
    public async Task Resolve_RecordsTheBatchWithItsResolvedContext(bool? usePrecomputedGenotypeSets)
    {
        var request = Request(usePrecomputedGenotypeSets);

        var context = await resolver.Resolve(request, Donors);

        readRecorder.Received(1).Record(request, Donors, context);
    }

    [Test]
    public async Task Resolve_WhenTheReadFails_StillRecordsTheBatch()
    {
        reader.GetDonorGenotypeSets(default, default, default, default).ThrowsAsyncForAnyArgs(new Exception("sql is down"));

        var context = await resolver.Resolve(Request(true), Donors);

        readRecorder.Received(1).Record(Arg.Any<IdentifiedMatchProbabilityRequest>(), Donors, context);
    }

    [Test]
    public async Task Resolve_WhenRecordingThrows_ReturnsTheContextRatherThanFailing()
    {
        readRecorder.WhenForAnyArgs(r => r.Record(default, default, default)).Throw(new Exception("recording broke"));

        var context = await resolver.Resolve(Request(true), Donors);

        context.BatchFallbackReason.Should().BeNull();
        context.AllowedLociKey.Should().Be("ABCDrb1Dqb1");
    }

    private static IdentifiedMatchProbabilityRequest Request(bool? usePrecomputedGenotypeSets) => new()
    {
        SearchRequestId = "search-id",
        MatchingAlgorithmDataRefreshRecordId = DataRefreshRecordId,
        UsePrecomputedGenotypeSets = usePrecomputedGenotypeSets
    };
}
