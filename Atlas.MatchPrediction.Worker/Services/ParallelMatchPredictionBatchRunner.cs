using Atlas.Common.AzureStorage.Blob;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Microsoft.Extensions.Options;

namespace Atlas.MatchPrediction.Worker.Services;

public interface IParallelMatchPredictionBatchRunner
{
    Task RunBatch(ParallelMatchPredictionBatchRequest request);
}

internal class ParallelMatchPredictionBatchRunner : IParallelMatchPredictionBatchRunner
{
    private readonly IBlobDownloader blobDownloader;
    private readonly IMatchPredictionAlgorithm matchPredictionAlgorithm;
    private readonly string requestsContainer;
    private readonly int maxDegreeOfParallelism;
    private readonly ILogger<ParallelMatchPredictionBatchRunner> logger;

    public ParallelMatchPredictionBatchRunner(
        IBlobDownloader blobDownloader,
        IMatchPredictionAlgorithm matchPredictionAlgorithm,
        IOptions<AzureStorageSettings> azureStorageSettings,
        IOptions<MatchPredictionRequestsSettings> matchPredictionRequestsSettings,
        ILogger<ParallelMatchPredictionBatchRunner> logger)
    {
        this.blobDownloader = blobDownloader;
        this.matchPredictionAlgorithm = matchPredictionAlgorithm;
        requestsContainer = azureStorageSettings.Value.MatchPredictionRequestsBlobContainer;
        maxDegreeOfParallelism = matchPredictionRequestsSettings.Value.MaxParallelism;
        this.logger = logger;
    }

    public async Task RunBatch(ParallelMatchPredictionBatchRequest request)
    {
        logger.LogInformation(
            "Downloading batch blob {BlobLocation} for search {SearchRequestId}",
            request.BlobLocation, request.SearchRequestId);

        var batchInput = await blobDownloader.Download<MultipleDonorMatchProbabilityInput>(requestsContainer, request.BlobLocation);

        logger.LogInformation(
            "Downloaded {DonorCount} donors for search {SearchRequestId}, blob {BlobLocation}",
            batchInput.Donors?.Count, request.SearchRequestId, request.BlobLocation);

        var results = await matchPredictionAlgorithm.RunMatchPredictionAlgorithmBatchParallel(batchInput, maxDegreeOfParallelism);

        logger.LogInformation(
            "Completed match prediction for {DonorCount} donors for search {SearchRequestId}, blob {BlobLocation}",
            results.Count, request.SearchRequestId, request.BlobLocation);

        // TODO: publish results to parallel-match-prediction-results with SessionId = request.SearchRequestId, TotalBatches = request.TotalBatches
    }
}
