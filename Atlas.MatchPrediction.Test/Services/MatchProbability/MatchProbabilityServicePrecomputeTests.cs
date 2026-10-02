using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.MatchPrediction.Services.Precompute;
using Atlas.MatchPrediction.Test.TestHelpers.Builders;
using Atlas.MatchPrediction.Test.TestHelpers.Builders.MatchProbabilityInputs;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;
using HaplotypeFrequencySet = Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet.HaplotypeFrequencySet;

namespace Atlas.MatchPrediction.Test.Services.MatchProbability;

/// <summary>
/// How <see cref="MatchProbabilityService"/> uses a batch's precomputed genotype sets (ATL-221).
/// </summary>
[TestFixture]
internal class MatchProbabilityServicePrecomputeTests
{
    private const int DonorFrequencySetId = 456;
    private const int OtherFrequencySetId = 789;
    private const int DataRefreshRecordId = 42;
    private const int DonorId = 1;

    private IGenotypeMatcher genotypeMatcher;
    private IMatchProbabilityService matchProbabilityService;
    private SubjectGenotypeSet patientGenotypeSet;
    private SubjectGenotypeSet liveDonorGenotypeSet;

    [SetUp]
    public void SetUp()
    {
        genotypeMatcher = Substitute.For<IGenotypeMatcher>();
        var haplotypeFrequencyService = Substitute.For<IHaplotypeFrequencyService>();
        var calculator = Substitute.For<IMatchProbabilityCalculator>();

        matchProbabilityService = new MatchProbabilityService(
            calculator,
            haplotypeFrequencyService,
            genotypeMatcher,
            Substitute.For<IMatchPredictionLogger<MatchProbabilityLoggingContext>>(),
            new MatchProbabilityLoggingContext());

        patientGenotypeSet = new SubjectGenotypeSet(false, new List<GenotypeAtDesiredResolutions>(), 0.1m);
        liveDonorGenotypeSet = SetWithGenotypes(3, 0.6m);

        haplotypeFrequencyService.GetHaplotypeFrequencySets(default, default).ReturnsForAnyArgs(new HaplotypeFrequencySetResponse
        {
            PatientSet = new HaplotypeFrequencySet { Id = 123 },
            DonorSet = new HaplotypeFrequencySet { Id = DonorFrequencySetId }
        });

        // Like the real matcher: uses the supplied donor set, or "computes" the live one.
        genotypeMatcher.MatchPatientDonorGenotypes(default).ReturnsForAnyArgs(call =>
        {
            var donorSet = call.Arg<GenotypeMatcherInput>().DonorGenotypeSet ?? liveDonorGenotypeSet;
            return new GenotypeMatcherResult
            {
                PatientResult = new GenotypeMatcherResult.SubjectResult(false, 1, 0.1m),
                DonorResult = new GenotypeMatcherResult.SubjectResult(donorSet.IsUnrepresented, donorSet.Genotypes.Count, donorSet.SumOfLikelihoods),
                GenotypeMatchDetails = new List<GenotypeMatchDetails>(),
                DonorGenotypeSet = donorSet
            };
        });

        calculator.CalculateMatchProbability(default, default, default, default).ReturnsForAnyArgs(new MatchProbabilityResponse());
    }

    [Test]
    public async Task CalculateMatchProbability_WithNoBatchContext_ComputesLiveAndStoresNothing()
    {
        var result = await matchProbabilityService.CalculateMatchProbability(Input(), patientGenotypeSet);

        result.GenotypeSetSource.Should().Be(DonorGenotypeSetSource.PrecomputeDisabled);
        result.GenotypeSetToStore.Should().BeNull();
        await genotypeMatcher.Received(1).MatchPatientDonorGenotypes(Arg.Is<GenotypeMatcherInput>(x => x.DonorGenotypeSet == null));
    }

    [Test]
    public async Task CalculateMatchProbability_WhenPrecomputeDisabled_ComputesLiveAndStoresNothing()
    {
        var context = DonorGenotypeSetBatchContext.Disabled(UsePrecomputedGenotypeSetsSource.FeatureFlag, DataRefreshRecordId);

        var result = await matchProbabilityService.CalculateMatchProbability(Input(), patientGenotypeSet, context);

        result.GenotypeSetSource.Should().Be(DonorGenotypeSetSource.PrecomputeDisabled);
        result.GenotypeSetToStore.Should().BeNull();
        await genotypeMatcher.Received(1).MatchPatientDonorGenotypes(Arg.Is<GenotypeMatcherInput>(x => x.DonorGenotypeSet == null));
    }

    [Test]
    public async Task CalculateMatchProbability_WithStoredRowForTheSearchFrequencySet_UsesDecodedSetAndStoresNothing()
    {
        var stored = SetWithGenotypes(2, 0.25m);
        var context = EnabledContext(new Dictionary<int, PrecomputedDonorGenotypeSetRow>
        {
            { DonorId, new PrecomputedDonorGenotypeSetRow(DonorFrequencySetId, false, SubjectGenotypeSetPayload.Encode(stored)) }
        });

        var result = await matchProbabilityService.CalculateMatchProbability(Input(), patientGenotypeSet, context);

        result.GenotypeSetSource.Should().Be(DonorGenotypeSetSource.Precomputed);
        result.GenotypeSetToStore.Should().BeNull();
        result.DonorGenotypeCount.Should().Be(2);
        await genotypeMatcher.Received(1).MatchPatientDonorGenotypes(Arg.Is<GenotypeMatcherInput>(x =>
            x.DonorGenotypeSet != null && x.DonorGenotypeSet.Genotypes.Count == 2 && x.DonorGenotypeSet.SumOfLikelihoods == 0.25m));
    }

    [Test]
    public async Task CalculateMatchProbability_WithStoredUnrepresentedRow_UsesUnrepresentedSet()
    {
        var context = EnabledContext(new Dictionary<int, PrecomputedDonorGenotypeSetRow>
        {
            { DonorId, new PrecomputedDonorGenotypeSetRow(DonorFrequencySetId, true, null) }
        });

        var result = await matchProbabilityService.CalculateMatchProbability(Input(), patientGenotypeSet, context);

        result.GenotypeSetSource.Should().Be(DonorGenotypeSetSource.Precomputed);
        result.Response.IsDonorPhenotypeUnrepresented.Should().BeTrue();
        await genotypeMatcher.Received(1).MatchPatientDonorGenotypes(Arg.Is<GenotypeMatcherInput>(x => x.DonorGenotypeSet.IsUnrepresented));
    }

    [Test]
    public async Task CalculateMatchProbability_WithRowOnlyForAnotherOfTheInputsDonorIds_UsesThatRow()
    {
        var input = Input();
        input.Donor.DonorIds = [DonorId, 2, 3];
        var context = EnabledContext(new Dictionary<int, PrecomputedDonorGenotypeSetRow>
        {
            { 3, new PrecomputedDonorGenotypeSetRow(DonorFrequencySetId, false, SubjectGenotypeSetPayload.Encode(SetWithGenotypes(1, 1m))) }
        });

        var result = await matchProbabilityService.CalculateMatchProbability(input, patientGenotypeSet, context);

        result.GenotypeSetSource.Should().Be(DonorGenotypeSetSource.Precomputed);
    }

    [Test]
    public async Task CalculateMatchProbability_WithNoRow_ComputesLiveAndReturnsSetToStore()
    {
        var input = Input();
        input.Donor.DonorIds = [DonorId, 2];
        var context = EnabledContext(new Dictionary<int, PrecomputedDonorGenotypeSetRow>());

        var result = await matchProbabilityService.CalculateMatchProbability(input, patientGenotypeSet, context);

        result.GenotypeSetSource.Should().Be(DonorGenotypeSetSource.NoRow);
        await genotypeMatcher.Received(1).MatchPatientDonorGenotypes(Arg.Is<GenotypeMatcherInput>(x => x.DonorGenotypeSet == null));

        var toStore = result.GenotypeSetToStore;
        toStore.Should().NotBeNull();
        toStore.DonorIds.Should().BeEquivalentTo([DonorId, 2]);
        toStore.HaplotypeFrequencySetId.Should().Be(DonorFrequencySetId);
        toStore.DonorHla.Should().Be(input.Donor.DonorHla.ToPhenotypeInfo());
        toStore.IsUnrepresented.Should().BeFalse();
        var decoded = SubjectGenotypeSetPayload.Decode(toStore.SubjectGenotypeSetData);
        decoded.Genotypes.Should().HaveCount(liveDonorGenotypeSet.Genotypes.Count);
        decoded.SumOfLikelihoods.Should().Be(liveDonorGenotypeSet.SumOfLikelihoods);
    }

    [Test]
    public async Task CalculateMatchProbability_WithRowForAnotherFrequencySet_ComputesLiveAndReturnsSetToStore()
    {
        var context = EnabledContext(new Dictionary<int, PrecomputedDonorGenotypeSetRow>
        {
            // Unrepresented under the old set: that flag must not be trusted either.
            { DonorId, new PrecomputedDonorGenotypeSetRow(OtherFrequencySetId, true, null) }
        });

        var result = await matchProbabilityService.CalculateMatchProbability(Input(), patientGenotypeSet, context);

        result.GenotypeSetSource.Should().Be(DonorGenotypeSetSource.StaleFrequencySet);
        result.Response.IsDonorPhenotypeUnrepresented.Should().BeFalse();
        result.GenotypeSetToStore.Should().NotBeNull();
        await genotypeMatcher.Received(1).MatchPatientDonorGenotypes(Arg.Is<GenotypeMatcherInput>(x => x.DonorGenotypeSet == null));
    }

    [Test]
    public async Task CalculateMatchProbability_WithUndecodableRow_ComputesLiveAndReturnsSetToStore()
    {
        var context = EnabledContext(new Dictionary<int, PrecomputedDonorGenotypeSetRow>
        {
            { DonorId, new PrecomputedDonorGenotypeSetRow(DonorFrequencySetId, false, [1, 2, 3]) }
        });

        var result = await matchProbabilityService.CalculateMatchProbability(Input(), patientGenotypeSet, context);

        result.GenotypeSetSource.Should().Be(DonorGenotypeSetSource.DecodeFailed);
        result.GenotypeSetToStore.Should().NotBeNull();
        await genotypeMatcher.Received(1).MatchPatientDonorGenotypes(Arg.Is<GenotypeMatcherInput>(x => x.DonorGenotypeSet == null));
    }

    [Test]
    public async Task CalculateMatchProbability_WhenLiveSetIsUnrepresented_ReturnsSetToStoreWithNoData()
    {
        liveDonorGenotypeSet = new SubjectGenotypeSet(true, new List<GenotypeAtDesiredResolutions>(), 0m);

        var result = await matchProbabilityService.CalculateMatchProbability(Input(), patientGenotypeSet, EnabledContext([]));

        result.GenotypeSetToStore.IsUnrepresented.Should().BeTrue();
        result.GenotypeSetToStore.SubjectGenotypeSetData.Should().BeNull();
    }

    [TestCase(DonorGenotypeSetSource.UncoveredAllowedLoci)]
    [TestCase(DonorGenotypeSetSource.ActiveDatabaseChanged)]
    [TestCase(DonorGenotypeSetSource.ActiveDatabaseUnknown)]
    [TestCase(DonorGenotypeSetSource.ReadFailed)]
    public async Task CalculateMatchProbability_WithBatchFallbackReason_ComputesLiveWithThatReasonAndStoresNothing(DonorGenotypeSetSource reason)
    {
        var context = DonorGenotypeSetBatchContext.Enabled(
            UsePrecomputedGenotypeSetsSource.Request,
            DataRefreshRecordId,
            PrecomputedDonorGenotypeSetLookup.Unavailable(reason));

        var result = await matchProbabilityService.CalculateMatchProbability(Input(), patientGenotypeSet, context);

        result.GenotypeSetSource.Should().Be(reason);
        result.GenotypeSetToStore.Should().BeNull();
        await genotypeMatcher.Received(1).MatchPatientDonorGenotypes(Arg.Is<GenotypeMatcherInput>(x => x.DonorGenotypeSet == null));
    }

    private static SingleDonorMatchProbabilityInput Input() => SingleDonorMatchProbabilityInputBuilder.Valid.WithDonorId(DonorId).Build();

    private static DonorGenotypeSetBatchContext EnabledContext(Dictionary<int, PrecomputedDonorGenotypeSetRow> rows) =>
        DonorGenotypeSetBatchContext.Enabled(
            UsePrecomputedGenotypeSetsSource.FeatureFlag,
            DataRefreshRecordId,
            new PrecomputedDonorGenotypeSetLookup("ABCDrb1Dqb1", null, rows));

    private static SubjectGenotypeSet SetWithGenotypes(int count, decimal sumOfLikelihoods) =>
        new(
            false,
            Enumerable.Range(0, count)
                .Select(i => new GenotypeAtDesiredResolutionsBuilder().Default().WithLikelihood(0.1m * (i + 1)).Build())
                .ToList(),
            sumOfLikelihoods);
}
