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
    private MatchPredictionInputBuilder builder;

    private Fixture fixture;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        donorInputBatcher = Substitute.For<IDonorInputBatcher>();
        var logger = Substitute.For<ISearchLogger<SearchLoggingContext>>();

        builder = new MatchPredictionInputBuilder(
            logger,
            donorInputBatcher,
            Options.Create(new OrchestrationSettings { MatchPredictionBatchSize = ConfiguredBatchSize }));
    }

    [Test]
    public void BuildMatchPredictionInputs_WhenNoOverrideProvided_BatchesUsingConfiguredBatchSize()
    {
        var resultSet = BuildResultSet();

        builder.BuildMatchPredictionInputs(resultSet);

        donorInputBatcher.Received(1).BatchDonorInputs(Arg.Any<IdentifiedMatchProbabilityRequest>(), Arg.Any<IEnumerable<DonorInput>>(), ConfiguredBatchSize);
    }

    [Test]
    public void BuildMatchPredictionInputs_WhenOverrideProvided_BatchesUsingOverride()
    {
        var resultSet = BuildResultSet();
        const int overrideBatchSize = 500;

        builder.BuildMatchPredictionInputs(resultSet, overrideBatchSize);

        donorInputBatcher.Received(1).BatchDonorInputs(Arg.Any<IdentifiedMatchProbabilityRequest>(), Arg.Any<IEnumerable<DonorInput>>(), overrideBatchSize);
    }

    // The override is applied only when strictly positive; a zero override (the sequential path's unset default)
    // must fall back to the configured batch size rather than batching everything into a single zero-sized batch.
    [TestCase(0)]
    [TestCase(-1)]
    public void BuildMatchPredictionInputs_WhenOverrideIsNotPositive_FallsBackToConfiguredBatchSize(int nonPositiveOverride)
    {
        var resultSet = BuildResultSet();

        builder.BuildMatchPredictionInputs(resultSet, nonPositiveOverride);

        donorInputBatcher.Received(1).BatchDonorInputs(Arg.Any<IdentifiedMatchProbabilityRequest>(), Arg.Any<IEnumerable<DonorInput>>(), ConfiguredBatchSize);
    }

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
