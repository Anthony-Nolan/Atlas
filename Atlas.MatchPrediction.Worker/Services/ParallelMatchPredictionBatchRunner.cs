using Atlas.Common.AzureStorage.Blob;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.SearchTracking.Common.Dispatchers;
using Microsoft.Extensions.Options;

namespace Atlas.MatchPrediction.Worker.Services;

public interface IParallelMatchPredictionBatchRunner
{
    Task RunBatch(ParallelMatchPredictionBatchRequest request);
}

internal class ParallelMatchPredictionBatchRunner : IParallelMatchPredictionBatchRunner
{
    private readonly IBlobDownloader blobDownloader;
    private readonly IParallelMatchPredictionAlgorithm parallelMatchPredictionAlgorithm;
    private readonly IMatchPredictionSearchTrackingDispatcher trackingDispatcher;
    private readonly string requestsContainer;
    private readonly int maxDegreeOfParallelism;
    private readonly ILogger<ParallelMatchPredictionBatchRunner> logger;

    public ParallelMatchPredictionBatchRunner(
        IBlobDownloader blobDownloader,
        IParallelMatchPredictionAlgorithm parallelMatchPredictionAlgorithm,
        IMatchPredictionSearchTrackingDispatcher trackingDispatcher,
        IOptions<AzureStorageSettings> azureStorageSettings,
        IOptions<MatchPredictionRequestsSettings> matchPredictionRequestsSettings,
        ILogger<ParallelMatchPredictionBatchRunner> logger)
    {
        this.blobDownloader = blobDownloader;
        this.parallelMatchPredictionAlgorithm = parallelMatchPredictionAlgorithm;
        this.trackingDispatcher = trackingDispatcher;
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

        var searchIdentifier = Guid.Parse(request.SearchRequestId);
        var originalSearchIdentifier = request.IsRepeatSearch
            ? Guid.Parse(request.RepeatSearchRequestId)
            : (Guid?)null;

        await trackingDispatcher.ProcessRunningBatchesStarted(searchIdentifier, originalSearchIdentifier);

        var results = await parallelMatchPredictionAlgorithm.RunBatch(batchInput, maxDegreeOfParallelism);

        await trackingDispatcher.ProcessRunningBatchesEnded(searchIdentifier, originalSearchIdentifier);

        logger.LogInformation(
            "Completed match prediction for {DonorCount} donors for search {SearchRequestId}, blob {BlobLocation}",
            results.Count, request.SearchRequestId, request.BlobLocation);

        // TODO: publish results to parallel-match-prediction-results with SessionId = request.SearchRequestId, TotalBatches = request.TotalBatches
    }
}
