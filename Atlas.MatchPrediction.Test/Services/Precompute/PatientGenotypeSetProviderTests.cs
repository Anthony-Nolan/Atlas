using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.MatchPrediction.Services.Precompute;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;
using HaplotypeFrequencySet = Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet.HaplotypeFrequencySet;

namespace Atlas.MatchPrediction.Test.Services.Precompute;

[TestFixture]
internal class PatientGenotypeSetProviderTests
{
    private const int PatientFrequencySetId = 7;
    private static readonly PatientGenotypeSetKey Key = new("typing-key", PatientFrequencySetId, "ABCDrb1Dqb1");

    private IGenotypeSetService genotypeSetService;
    private IHaplotypeFrequencyLookupService haplotypeFrequencyService;
    private IPatientGenotypeSetProvider provider;

    private SubjectGenotypeSet liveGenotypeSet;
    private HaplotypeFrequencySet patientFrequencySet;

    [SetUp]
    public void SetUp()
    {
        genotypeSetService = Substitute.For<IGenotypeSetService>();
        haplotypeFrequencyService = Substitute.For<IHaplotypeFrequencyLookupService>();
        provider = new PatientGenotypeSetProvider(genotypeSetService, haplotypeFrequencyService);

        patientFrequencySet = new HaplotypeFrequencySet { Id = PatientFrequencySetId };
        haplotypeFrequencyService.GetSingleHaplotypeFrequencySet(default).ReturnsForAnyArgs(patientFrequencySet);

        liveGenotypeSet = new SubjectGenotypeSet(false, new List<GenotypeAtDesiredResolutions>(), 0.5m);
        genotypeSetService.GetPatientGenotypeSet(default, default).ReturnsForAnyArgs(liveGenotypeSet);
    }

    [Test]
    public async Task Get_WhenTheStoredRowIsUsable_DecodesItAndDoesNotImpute()
    {
        var stored = SubjectGenotypeSetPayloadTests.CanonicalV1Set();
        var context = Enabled(new PrecomputedPatientGenotypeSetRow(false, SubjectGenotypeSetPayload.Encode(stored)));

        var (genotypeSet, source) = await provider.Get(Input(), context);

        source.Should().Be(PatientGenotypeSetSource.Precomputed);
        genotypeSet.Genotypes.Should().HaveCount(stored.Genotypes.Count);
        genotypeSet.SumOfLikelihoods.Should().Be(stored.SumOfLikelihoods);
        await genotypeSetService.DidNotReceiveWithAnyArgs().GetPatientGenotypeSet(default, default);
        await genotypeSetService.DidNotReceiveWithAnyArgs().GetPatientGenotypeSet(default);
    }

    [Test]
    public async Task Get_WhenTheStoredRowIsUnrepresented_ReturnsAnUnrepresentedSetWithoutImputing()
    {
        var context = Enabled(new PrecomputedPatientGenotypeSetRow(true, null));

        var (genotypeSet, source) = await provider.Get(Input(), context);

        source.Should().Be(PatientGenotypeSetSource.Precomputed);
        genotypeSet.IsUnrepresented.Should().BeTrue();
        genotypeSet.Genotypes.Should().BeEmpty();
        await genotypeSetService.DidNotReceiveWithAnyArgs().GetPatientGenotypeSet(default, default);
    }

    [Test]
    public async Task Get_WhenThePatientFrequencySetChangedSinceTheWarmStep_ComputesLiveWithTheBatchesSet()
    {
        patientFrequencySet.Id = PatientFrequencySetId + 1;
        var context = Enabled(new PrecomputedPatientGenotypeSetRow(false, SubjectGenotypeSetPayload.Encode(SubjectGenotypeSetPayloadTests.CanonicalV1Set())));
        var input = Input();

        var (genotypeSet, source) = await provider.Get(input, context);

        source.Should().Be(PatientGenotypeSetSource.StaleFrequencySet);
        genotypeSet.Should().BeSameAs(liveGenotypeSet);
        await genotypeSetService.Received(1).GetPatientGenotypeSet(input, patientFrequencySet);
    }

    [Test]
    public async Task Get_WhenTheKeyHasNoRow_ComputesLive()
    {
        var (genotypeSet, source) = await provider.Get(Input(), Enabled(null));

        source.Should().Be(PatientGenotypeSetSource.NoRow);
        genotypeSet.Should().BeSameAs(liveGenotypeSet);
    }

    [Test]
    public async Task Get_WhenTheBatchHasNoKey_ComputesLive()
    {
        var context = DonorGenotypeSetBatchContext.Enabled(
            UsePrecomputedGenotypeSetsSource.FeatureFlag, PrecomputedGenotypeSetMode.DefaultPrecomputed, 42, Lookup(null), patientGenotypeSetKey: null);

        var (genotypeSet, source) = await provider.Get(Input(), context);

        source.Should().Be(PatientGenotypeSetSource.NoKey);
        genotypeSet.Should().BeSameAs(liveGenotypeSet);
    }

    [TestCase(new byte[] { 1, 2, 3 })]
    [TestCase(null)]
    public async Task Get_WhenTheStoredRowCannotBeDecoded_ComputesLive(byte[] payload)
    {
        var (genotypeSet, source) = await provider.Get(Input(), Enabled(new PrecomputedPatientGenotypeSetRow(false, payload)));

        source.Should().Be(PatientGenotypeSetSource.DecodeFailed);
        genotypeSet.Should().BeSameAs(liveGenotypeSet);
    }

    [TestCase(DonorGenotypeSetSource.ActiveDatabaseChanged, PatientGenotypeSetSource.ActiveDatabaseChanged)]
    [TestCase(DonorGenotypeSetSource.ActiveDatabaseUnknown, PatientGenotypeSetSource.ActiveDatabaseUnknown)]
    [TestCase(DonorGenotypeSetSource.UncoveredAllowedLoci, PatientGenotypeSetSource.UncoveredAllowedLoci)]
    [TestCase(DonorGenotypeSetSource.ReadFailed, PatientGenotypeSetSource.ReadFailed)]
    public async Task Get_WhenTheBatchHasAFallbackReason_ComputesLiveForTheSameReason(
        DonorGenotypeSetSource batchReason,
        PatientGenotypeSetSource expectedSource)
    {
        var context = DonorGenotypeSetBatchContext.Enabled(
            UsePrecomputedGenotypeSetsSource.FeatureFlag,
            PrecomputedGenotypeSetMode.DefaultPrecomputed,
            42,
            PrecomputedDonorGenotypeSetLookup.Unavailable(batchReason),
            Key);

        var (genotypeSet, source) = await provider.Get(Input(), context);

        source.Should().Be(expectedSource);
        genotypeSet.Should().BeSameAs(liveGenotypeSet);
    }

    [Test]
    public async Task Get_WhenThePrecomputedPathIsOff_ComputesLiveEvenWithAKey()
    {
        var context = DonorGenotypeSetBatchContext.Disabled(UsePrecomputedGenotypeSetsSource.FeatureFlag, PrecomputedGenotypeSetMode.ForceLive, 42, Key);

        var (genotypeSet, source) = await provider.Get(Input(), context);

        source.Should().Be(PatientGenotypeSetSource.PrecomputeDisabled);
        genotypeSet.Should().BeSameAs(liveGenotypeSet);
    }

    [Test]
    public async Task Get_LooksUpThePatientFrequencySetFromThePatientMetadata()
    {
        var metadata = new FrequencySetMetadata { RegistryCode = "reg", EthnicityCode = "eth" };
        var input = Input();
        input.PatientFrequencySetMetadata = metadata;

        await provider.Get(input, Enabled(null));

        await haplotypeFrequencyService.Received(1).GetSingleHaplotypeFrequencySet(metadata);
    }

    private static SingleDonorMatchProbabilityInput Input() => new() { SearchRequestId = "search-id" };

    private static DonorGenotypeSetBatchContext Enabled(PrecomputedPatientGenotypeSetRow patientRow) =>
        DonorGenotypeSetBatchContext.Enabled(
            UsePrecomputedGenotypeSetsSource.FeatureFlag, PrecomputedGenotypeSetMode.DefaultPrecomputed, 42, Lookup(patientRow), Key);

    private static PrecomputedDonorGenotypeSetLookup Lookup(PrecomputedPatientGenotypeSetRow patientRow) =>
        new("ABCDrb1Dqb1", null, new Dictionary<int, PrecomputedDonorGenotypeSetRow>(), patientRow);
}
