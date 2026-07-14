using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.Functions.Services;
using Atlas.Functions.Services.MatchCategories;
using Atlas.Functions.Settings;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.Functions.Test.Services;

[TestFixture]
internal class ResultsCombinerTests
{
    private const string MatchPredictionResultsContainer = "match-prediction-results";
    private const int DownloadBatchSize = 4;

    private IBlobDownloader blobDownloader;
    private IPositionalMatchCategoryService matchCategoryService;
    private ResultsCombiner combiner;

    private Fixture fixture;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        blobDownloader = Substitute.For<IBlobDownloader>();
        matchCategoryService = Substitute.For<IPositionalMatchCategoryService>();
        var logger = Substitute.For<ISearchLogger<SearchLoggingContext>>();

        combiner = new ResultsCombiner(
            Options.Create(new AzureStorageSettings
            {
                MatchPredictionResultsBlobContainer = MatchPredictionResultsContainer,
                MatchPredictionDownloadBatchSize = DownloadBatchSize
            }),
            logger,
            blobDownloader,
            matchCategoryService);
    }

    [Test]
    public void CombineResults_PairsEachMatchingResultWithItsMatchPredictionResultByDonorId()
    {
        var firstResult = BuildMatchingResult(atlasDonorId: 1);
        var secondResult = BuildMatchingResult(atlasDonorId: 2);
        var predictionByDonorId = new Dictionary<int, MatchProbabilityResponse>
        {
            [1] = new(new Probability(0.1m), new HashSet<Locus>()),
            [2] = new(new Probability(0.2m), new HashSet<Locus>())
        };
        var reorientatedFirst = new MatchProbabilityResponse();
        var reorientatedSecond = new MatchProbabilityResponse();
        matchCategoryService.ReOrientatePositionalMatchCategories(predictionByDonorId[1], firstResult.ScoringResult).Returns(reorientatedFirst);
        matchCategoryService.ReOrientatePositionalMatchCategories(predictionByDonorId[2], secondResult.ScoringResult).Returns(reorientatedSecond);

        var combined = combiner.CombineResults(fixture.Create<string>(), new[] { firstResult, secondResult }, predictionByDonorId).ToList();

        combined.Should().HaveCount(2);
        combined[0].DonorCode.Should().Be(firstResult.MatchingDonorInfo.ExternalDonorCode);
        combined[0].MatchingResult.Should().BeSameAs(firstResult);
        combined[0].MatchPredictionResult.Should().BeSameAs(reorientatedFirst);
        combined[1].DonorCode.Should().Be(secondResult.MatchingDonorInfo.ExternalDonorCode);
        combined[1].MatchPredictionResult.Should().BeSameAs(reorientatedSecond);
    }

    [Test]
    public void CombineResults_WhenMatchingResultHasNoMatchingPrediction_Throws()
    {
        var matchingResult = BuildMatchingResult(atlasDonorId: 1);
        // Prediction map is missing donor 1 — e.g. a failed or partial batch. The combiner indexes into the map,
        // so this surfaces as a KeyNotFoundException rather than a silently dropped or null-prediction donor.
        var predictionByDonorId = new Dictionary<int, MatchProbabilityResponse>
        {
            [999] = new(new Probability(0.5m), new HashSet<Locus>())
        };

        var combine = () => combiner.CombineResults(fixture.Create<string>(), new[] { matchingResult }, predictionByDonorId).ToList();

        combine.Should().Throw<KeyNotFoundException>();
    }

    [Test]
    public async Task DownloadBatchedMatchPredictionResults_MergesEveryBatchBlobIntoSingleDonorKeyedMap()
    {
        var firstBatchLocation = fixture.Create<string>();
        var secondBatchLocation = fixture.Create<string>();
        var firstResponse = new MatchProbabilityResponse();
        var secondResponse = new MatchProbabilityResponse();
        var thirdResponse = new MatchProbabilityResponse();
        blobDownloader.Download<Dictionary<int, MatchProbabilityResponse>>(MatchPredictionResultsContainer, firstBatchLocation)
            .Returns(new Dictionary<int, MatchProbabilityResponse> { [1] = firstResponse, [2] = secondResponse });
        blobDownloader.Download<Dictionary<int, MatchProbabilityResponse>>(MatchPredictionResultsContainer, secondBatchLocation)
            .Returns(new Dictionary<int, MatchProbabilityResponse> { [3] = thirdResponse });

        var merged = await combiner.DownloadBatchedMatchPredictionResults(new[] { firstBatchLocation, secondBatchLocation });

        merged.Should().HaveCount(3);
        merged[1].Should().BeSameAs(firstResponse);
        merged[2].Should().BeSameAs(secondResponse);
        merged[3].Should().BeSameAs(thirdResponse);
    }

    [Test]
    public async Task DownloadBatchedMatchPredictionResults_WhenNoBatches_ReturnsEmptyMapWithoutDownloading()
    {
        var merged = await combiner.DownloadBatchedMatchPredictionResults(Array.Empty<string>());

        merged.Should().BeEmpty();
        await blobDownloader.DidNotReceiveWithAnyArgs().Download<Dictionary<int, MatchProbabilityResponse>>(default, default);
    }

    private MatchingAlgorithmResult BuildMatchingResult(int atlasDonorId) =>
        new()
        {
            AtlasDonorId = atlasDonorId,
            ScoringResult = new ScoringResult(),
            MatchingDonorInfo = new MatchingDonorInfo { ExternalDonorCode = fixture.Create<string>() }
        };
}
