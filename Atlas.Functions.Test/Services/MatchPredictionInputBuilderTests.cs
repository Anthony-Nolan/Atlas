using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Common.Requests;
using Atlas.Client.Models.Search.Requests;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Functions.Services;
using Atlas.Functions.Settings;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Services.Precompute;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.Functions.Test.Services;

[TestFixture]
internal class MatchPredictionInputBuilderTests
{
    private const int ConfiguredBatchSize = 25;

    private IDonorInputBatcher donorInputBatcher;
    private IPatientGenotypeSetWarmer patientGenotypeSetWarmer;
    private MatchPredictionInputBuilder builder;

    private Fixture fixture;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        donorInputBatcher = Substitute.For<IDonorInputBatcher>();
        patientGenotypeSetWarmer = Substitute.For<IPatientGenotypeSetWarmer>();
        var logger = Substitute.For<ISearchLogger<SearchLoggingContext>>();

        builder = new MatchPredictionInputBuilder(
            logger,
            donorInputBatcher,
            patientGenotypeSetWarmer,
            Options.Create(new OrchestrationSettings { MatchPredictionBatchSize = ConfiguredBatchSize }));
    }

    [Test]
    public async Task BuildMatchPredictionInputs_WhenNoOverrideProvided_BatchesUsingConfiguredBatchSize()
    {
        var resultSet = BuildResultSet();

        await builder.BuildMatchPredictionInputs(resultSet);

        donorInputBatcher.Received(1)
            .BatchDonorInputs(Arg.Any<IdentifiedMatchProbabilityRequest>(), Arg.Any<IEnumerable<DonorInput>>(), ConfiguredBatchSize);
    }

    [Test]
    public async Task BuildMatchPredictionInputs_WhenOverrideProvided_BatchesUsingOverride()
    {
        var resultSet = BuildResultSet();
        const int overrideBatchSize = 500;

        await builder.BuildMatchPredictionInputs(resultSet, overrideBatchSize);

        donorInputBatcher.Received(1)
            .BatchDonorInputs(Arg.Any<IdentifiedMatchProbabilityRequest>(), Arg.Any<IEnumerable<DonorInput>>(), overrideBatchSize);
    }

    // The override is applied only when strictly positive; a zero override (the sequential path's unset default)
    // must fall back to the configured batch size rather than batching everything into a single zero-sized batch.
    [TestCase(0)]
    [TestCase(-1)]
    public async Task BuildMatchPredictionInputs_WhenOverrideIsNotPositive_FallsBackToConfiguredBatchSize(int nonPositiveOverride)
    {
        var resultSet = BuildResultSet();

        await builder.BuildMatchPredictionInputs(resultSet, nonPositiveOverride);

        donorInputBatcher.Received(1)
            .BatchDonorInputs(Arg.Any<IdentifiedMatchProbabilityRequest>(), Arg.Any<IEnumerable<DonorInput>>(), ConfiguredBatchSize);
    }

    [TestCase(42)]
    [TestCase(null)]
    public async Task BuildMatchPredictionInputs_CopiesDataRefreshRecordIdFromResultSet(int? dataRefreshRecordId)
    {
        var resultSet = BuildResultSet();
        resultSet.MatchingAlgorithmDataRefreshRecordId = dataRefreshRecordId;

        await builder.BuildMatchPredictionInputs(resultSet);

        donorInputBatcher.Received(1).BatchDonorInputs(
            Arg.Is<IdentifiedMatchProbabilityRequest>(r => r.MatchingAlgorithmDataRefreshRecordId == dataRefreshRecordId),
            Arg.Any<IEnumerable<DonorInput>>(),
            Arg.Any<int>());
    }

    [TestCase(true)]
    [TestCase(false)]
    [TestCase(null)]
    public async Task BuildMatchPredictionInputs_PassesUsePrecomputedGenotypeSetsOnUnresolved(bool? usePrecomputedGenotypeSets)
    {
        var resultSet = BuildResultSet();
        resultSet.SearchRequest.UsePrecomputedGenotypeSets = usePrecomputedGenotypeSets;

        await builder.BuildMatchPredictionInputs(resultSet);

        donorInputBatcher.Received(1).BatchDonorInputs(
            Arg.Is<IdentifiedMatchProbabilityRequest>(r => r.UsePrecomputedGenotypeSets == usePrecomputedGenotypeSets),
            Arg.Any<IEnumerable<DonorInput>>(),
            Arg.Any<int>());
    }

    [Test]
    public async Task BuildMatchPredictionInputs_WarmsThePatientSetOnceAndPutsItsKeyOnTheRequestEveryBatchIsCopiedFrom()
    {
        var resultSet = BuildResultSet();
        resultSet.MatchingAlgorithmDataRefreshRecordId = 42;
        resultSet.Results = [BuildResult(1)];
        var key = new PatientGenotypeSetKey("typing-key", 7, "ABCDrb1Dqb1");
        patientGenotypeSetWarmer.Warm(default, default).ReturnsForAnyArgs(key);

        await builder.BuildMatchPredictionInputs(resultSet);

        await patientGenotypeSetWarmer.Received(1).Warm(
            Arg.Is<IdentifiedMatchProbabilityRequest>(r => r.SearchRequestId == resultSet.SearchRequestId && r.MatchingAlgorithmDataRefreshRecordId == 42),
            true);
        donorInputBatcher.Received(1).BatchDonorInputs(
            Arg.Is<IdentifiedMatchProbabilityRequest>(r => r.PatientGenotypeSetKey == key),
            Arg.Any<IEnumerable<DonorInput>>(),
            Arg.Any<int>());
    }

    [Test]
    public async Task BuildMatchPredictionInputs_WhenTheWarmStepGivesNoKey_LeavesTheKeyNull()
    {
        patientGenotypeSetWarmer.Warm(default, default).ReturnsForAnyArgs((PatientGenotypeSetKey)null);

        await builder.BuildMatchPredictionInputs(BuildResultSet());

        donorInputBatcher.Received(1).BatchDonorInputs(
            Arg.Is<IdentifiedMatchProbabilityRequest>(r => r.PatientGenotypeSetKey == null),
            Arg.Any<IEnumerable<DonorInput>>(),
            Arg.Any<int>());
    }

    [Test]
    public async Task BuildMatchPredictionInputs_WhenMatchingFoundNoDonors_TellsTheWarmStepThereAreNone()
    {
        var resultSet = BuildResultSet();
        resultSet.Results = [];

        await builder.BuildMatchPredictionInputs(resultSet);

        await patientGenotypeSetWarmer.Received(1).Warm(Arg.Any<IdentifiedMatchProbabilityRequest>(), false);
    }

    private static MatchingAlgorithmResult BuildResult(int donorId) => new()
    {
        AtlasDonorId = donorId,
        MatchingResult = new MatchingResult { DonorHla = new PhenotypeInfoTransfer<string>() },
        MatchingDonorInfo = new MatchingDonorInfo { EthnicityCode = "eth", RegistryCode = "reg" }
    };

    private OriginalMatchingAlgorithmResultSet BuildResultSet() =>
        new()
        {
            SearchRequestId = fixture.Create<string>(),
            MatchingAlgorithmHlaNomenclatureVersion = fixture.Create<string>(),
            Results = new List<MatchingAlgorithmResult>(),
            SearchRequest = new SearchRequest
            {
                MatchCriteria = new MismatchCriteria { LocusMismatchCriteria = new LociInfoTransfer<int?>() },
                SearchHlaData = new PhenotypeInfoTransfer<string>(),
                PatientEthnicityCode = fixture.Create<string>(),
                PatientRegistryCode = fixture.Create<string>()
            }
        };
}
