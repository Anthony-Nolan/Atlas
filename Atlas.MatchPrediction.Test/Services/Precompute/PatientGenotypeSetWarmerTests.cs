using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.MatchPrediction.Services.Precompute;
using AwesomeAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using HaplotypeFrequencySet = Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet.HaplotypeFrequencySet;

namespace Atlas.MatchPrediction.Test.Services.Precompute;

[TestFixture]
internal class PatientGenotypeSetWarmerTests
{
    private const int DataRefreshRecordId = 42;
    private const int PatientFrequencySetId = 7;
    private static readonly PatientGenotypeSetKey Key = new("typing-key", PatientFrequencySetId, "ABCDrb1Dqb1");

    private PrecomputedGenotypeSetSettings settings;
    private IHaplotypeFrequencyLookupService haplotypeFrequencyService;
    private IGenotypeSetService genotypeSetService;
    private IPrecomputedDonorGenotypeSetReader reader;
    private IPrecomputedDonorGenotypeSetWriter writer;
    private IAtlasLogger logger;
    private IPatientGenotypeSetWarmer warmer;

    private HaplotypeFrequencySet patientFrequencySet;
    private Dictionary<string, string> loggedProps;
    private Dictionary<string, double> loggedMetrics;

    [SetUp]
    public void SetUp()
    {
        settings = new PrecomputedGenotypeSetSettings { Mode = PrecomputedGenotypeSetMode.DefaultPrecomputed };
        haplotypeFrequencyService = Substitute.For<IHaplotypeFrequencyLookupService>();
        genotypeSetService = Substitute.For<IGenotypeSetService>();
        reader = Substitute.For<IPrecomputedDonorGenotypeSetReader>();
        writer = Substitute.For<IPrecomputedDonorGenotypeSetWriter>();
        logger = Substitute.For<IAtlasLogger>();
        warmer = new PatientGenotypeSetWarmer(settings, haplotypeFrequencyService, genotypeSetService, reader, writer, logger);

        patientFrequencySet = new HaplotypeFrequencySet { Id = PatientFrequencySetId };
        haplotypeFrequencyService.GetSingleHaplotypeFrequencySet(default).ReturnsForAnyArgs(patientFrequencySet);
        reader.FindPatientGenotypeSet(default, default, default, default).ReturnsForAnyArgs(new PatientGenotypeSetLookup(Key, false, null));
        genotypeSetService.GetPatientGenotypeSet(default, default)
            .ReturnsForAnyArgs(SubjectGenotypeSetPayloadTests.CanonicalV1Set());
        writer.StorePatientGenotypeSet(default, default, default, default).ReturnsForAnyArgs(true);

        logger.When(l => l.SendEvent(PatientGenotypeSetWarmer.WarmedEventName, Arg.Any<LogLevel>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<Dictionary<string, double>>()))
            .Do(call =>
            {
                loggedProps = call.ArgAt<Dictionary<string, string>>(2);
                loggedMetrics = call.ArgAt<Dictionary<string, double>>(3);
            });
    }

    [Test]
    public async Task Warm_WhenTheSetIsNotStored_ComputesItWithTheResolvedFrequencySetStoresItAndReturnsTheKey()
    {
        var request = Request();
        var expectedSet = SubjectGenotypeSetPayloadTests.CanonicalV1Set();

        var key = await warmer.Warm(request);

        key.Should().Be(Key);
        await genotypeSetService.Received(1).GetPatientGenotypeSet(
            Arg.Is<SingleDonorMatchProbabilityInput>(i => i.SearchRequestId == request.SearchRequestId),
            patientFrequencySet);
        await writer.Received(1).StorePatientGenotypeSet(
            Key,
            false,
            Arg.Is<byte[]>(data => data.SequenceEqual(SubjectGenotypeSetPayload.Encode(expectedSet))),
            DataRefreshRecordId);
        AssertEvent("Created", null);
        loggedMetrics["PatientGenotypeCount"].Should().Be(expectedSet.Genotypes.Count);
    }

    [Test]
    public async Task Warm_LooksUpTheKeyWithThePatientsTypingFrequencySetLociAndRecord()
    {
        var request = Request();
        request.ExcludedLoci = [Locus.C];

        await warmer.Warm(request);

        await haplotypeFrequencyService.Received(1).GetSingleHaplotypeFrequencySet(request.PatientFrequencySetMetadata);
        await reader.Received(1).FindPatientGenotypeSet(
            Arg.Is<PhenotypeInfo<string>>(hla => hla.GetPosition(Locus.A, LocusPosition.One) == "A*01:01"),
            PatientFrequencySetId,
            Arg.Is<IReadOnlySet<Locus>>(loci => loci.SetEquals(new[] { Locus.A, Locus.B, Locus.Dqb1, Locus.Drb1 })),
            DataRefreshRecordId);
    }

    [Test]
    public async Task Warm_WhenTheSetIsAlreadyStored_ReturnsTheKeyWithoutComputingOrStoring()
    {
        reader.FindPatientGenotypeSet(default, default, default, default).ReturnsForAnyArgs(new PatientGenotypeSetLookup(Key, true, null));

        var key = await warmer.Warm(Request());

        key.Should().Be(Key);
        await genotypeSetService.DidNotReceiveWithAnyArgs().GetPatientGenotypeSet(default, default);
        await writer.DidNotReceiveWithAnyArgs().StorePatientGenotypeSet(default, default, default, default);
        AssertEvent("Hit", null);
    }

    [Test]
    public async Task Warm_WhenThePatientIsUnrepresented_StoresTheRowWithNoData()
    {
        genotypeSetService.GetPatientGenotypeSet(default, default)
            .ReturnsForAnyArgs(new SubjectGenotypeSet(true, new List<GenotypeAtDesiredResolutions>(), 0m));

        var key = await warmer.Warm(Request());

        key.Should().Be(Key);
        await writer.Received(1).StorePatientGenotypeSet(Key, true, null, DataRefreshRecordId);
    }

    [TestCase(PrecomputedGenotypeSetMode.ForceLive, true)]
    [TestCase(PrecomputedGenotypeSetMode.DefaultLive, null)]
    [TestCase(PrecomputedGenotypeSetMode.DefaultPrecomputed, false)]
    public async Task Warm_WhenThePrecomputedPathIsOffForTheSearch_DoesNothingAndReturnsNull(PrecomputedGenotypeSetMode mode, bool? requestOverride)
    {
        settings.Mode = mode;
        var request = Request();
        request.UsePrecomputedGenotypeSets = requestOverride;

        var key = await warmer.Warm(request);

        key.Should().BeNull();
        await reader.DidNotReceiveWithAnyArgs().FindPatientGenotypeSet(default, default, default, default);
        await writer.DidNotReceiveWithAnyArgs().StorePatientGenotypeSet(default, default, default, default);
        AssertEvent("Skipped", "PrecomputeDisabled");
    }

    [TestCase(DonorGenotypeSetSource.ActiveDatabaseChanged)]
    [TestCase(DonorGenotypeSetSource.ActiveDatabaseUnknown)]
    [TestCase(DonorGenotypeSetSource.UncoveredAllowedLoci)]
    public async Task Warm_WhenTheLookupIsUnavailable_DoesNotComputeAndReturnsNull(DonorGenotypeSetSource reason)
    {
        reader.FindPatientGenotypeSet(default, default, default, default).ReturnsForAnyArgs(PatientGenotypeSetLookup.Unavailable(reason));

        var key = await warmer.Warm(Request());

        key.Should().BeNull();
        await genotypeSetService.DidNotReceiveWithAnyArgs().GetPatientGenotypeSet(default, default);
        AssertEvent("Skipped", reason.ToString());
    }

    [Test]
    public async Task Warm_WhenTheDatabaseChangesBeforeTheWrite_ReturnsNull()
    {
        writer.StorePatientGenotypeSet(default, default, default, default).ReturnsForAnyArgs(false);

        var key = await warmer.Warm(Request());

        key.Should().BeNull();
        AssertEvent("Skipped", "ActiveDatabaseChanged");
    }

    [Test]
    public async Task Warm_WhenComputingFails_LogsAndReturnsNullWithoutThrowing()
    {
        genotypeSetService.GetPatientGenotypeSet(default, default).ThrowsAsyncForAnyArgs(new Exception("hmd is down"));

        var act = () => warmer.Warm(Request());

        (await act.Should().NotThrowAsync()).Which.Should().BeNull();
        await writer.DidNotReceiveWithAnyArgs().StorePatientGenotypeSet(default, default, default, default);
        AssertEvent("Failed", null, LogLevel.Warn);
        loggedProps["ExceptionMessage"].Should().Be("hmd is down");
        logger.Received(1).SendException(Arg.Any<Exception>(), LogLevel.Warn, Arg.Any<Dictionary<string, string>>());
    }

    [Test]
    public async Task Warm_WhenStoringFails_LogsAndReturnsNullWithoutThrowing()
    {
        writer.StorePatientGenotypeSet(default, default, default, default).ThrowsAsyncForAnyArgs(new Exception("sql is down"));

        var act = () => warmer.Warm(Request());

        (await act.Should().NotThrowAsync()).Which.Should().BeNull();
        AssertEvent("Failed", null, LogLevel.Warn);
    }

    [Test]
    public async Task Warm_WhenLoggingFails_StillReturnsTheKey()
    {
        logger.WhenForAnyArgs(l => l.SendEvent(default, default, default, default)).Throw(new Exception("app insights is down"));

        var key = await warmer.Warm(Request());

        key.Should().Be(Key);
    }

    private void AssertEvent(string result, string reason, LogLevel level = LogLevel.Info)
    {
        logger.Received(1).SendEvent(PatientGenotypeSetWarmer.WarmedEventName, level, Arg.Any<Dictionary<string, string>>(), Arg.Any<Dictionary<string, double>>());
        loggedProps["SearchRequestId"].Should().Be("search-id");
        loggedProps["Result"].Should().Be(result);
        loggedProps["Reason"].Should().Be(reason);
        loggedProps["PrecomputeMode"].Should().Be(settings.Mode.ToString());
        loggedProps["MatchingAlgorithmDataRefreshRecordId"].Should().Be(DataRefreshRecordId.ToString());
    }

    private static IdentifiedMatchProbabilityRequest Request() => new()
    {
        SearchRequestId = "search-id",
        MatchingAlgorithmDataRefreshRecordId = DataRefreshRecordId,
        MatchingAlgorithmHlaNomenclatureVersion = "3330",
        PatientHla = new PhenotypeInfo<string>("A*01:01").ToPhenotypeInfoTransfer(),
        PatientFrequencySetMetadata = new FrequencySetMetadata { RegistryCode = "reg", EthnicityCode = "eth" }
    };
}
