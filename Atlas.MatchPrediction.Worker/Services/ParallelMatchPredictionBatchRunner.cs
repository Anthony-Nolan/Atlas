using Atlas.Common.AzureStorage.Blob;
using Atlas.Common.ServiceBus;
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
    private readonly ISessionMessagePublisher<ParallelMatchPredictionBatchResult> resultPublisher;
    private readonly IMatchPredictionSearchTrackingDispatcher trackingDispatcher;
    private readonly string requestsContainer;
    private readonly int maxDegreeOfParallelism;
    private readonly ILogger<ParallelMatchPredictionBatchRunner> logger;

    public ParallelMatchPredictionBatchRunner(
        IBlobDownloader blobDownloader,
        IParallelMatchPredictionAlgorithm parallelMatchPredictionAlgorithm,
        ISessionMessagePublisher<ParallelMatchPredictionBatchResult> resultPublisher,
        IMatchPredictionSearchTrackingDispatcher trackingDispatcher,
        IOptions<AzureStorageSettings> azureStorageSettings,
        IOptions<MatchPredictionRequestsSettings> matchPredictionRequestsSettings,
        ILogger<ParallelMatchPredictionBatchRunner> logger)
    {
        this.blobDownloader = blobDownloader;
        this.parallelMatchPredictionAlgorithm = parallelMatchPredictionAlgorithm;
        this.resultPublisher = resultPublisher;
        this.trackingDispatcher = trackingDispatcher;
        requestsContainer = azureStorageSettings.Value.MatchPredictionRequestsBlobContainer;
        maxDegreeOfParallelism = matchPredictionRequestsSettings.Value.MaxParallelism;
        this.logger = logger;
    }

    public async Task RunBatch(ParallelMatchPredictionBatchRequest request)
    {
        logger.LogInformation(
            "Downloading batch blob {BlobLocation} for search {SearchRequestId}",
            request.BlobLocation, request.SearchRequestId
        );

        var batchInput = await blobDownloader.Download<MultipleDonorMatchProbabilityInput>(requestsContainer, request.BlobLocation);

        logger.LogInformation(
            "Downloaded {DonorCount} donors for search {SearchRequestId}, blob {BlobLocation}",
            batchInput.Donors?.Count, request.SearchRequestId, request.BlobLocation
        );

        var searchIdentifier = request.IsRepeatSearch
            ? Guid.Parse(request.RepeatSearchRequestId)
            : Guid.Parse(request.SearchRequestId);
        var originalSearchIdentifier = request.IsRepeatSearch
            ? Guid.Parse(request.SearchRequestId)
            : (Guid?)null;

        await trackingDispatcher.ProcessRunningBatchesStarted(searchIdentifier, originalSearchIdentifier);

        var results = await parallelMatchPredictionAlgorithm.RunBatch(batchInput, maxDegreeOfParallelism);

        await trackingDispatcher.ProcessRunningBatchesEnded(searchIdentifier, originalSearchIdentifier);

        logger.LogInformation(
            "Completed match prediction for {DonorCount} donors for search {SearchRequestId}, blob {BlobLocation}",
            results.Count, request.SearchRequestId, request.BlobLocation
        );

        var batchResult = new ParallelMatchPredictionBatchResult
        {
            SearchIdentifier = new Guid(request.SearchRequestId),
            RepeatSearchIdentifier = request.RepeatSearchRequestId == null ? null : new Guid(request.RepeatSearchRequestId),
            MatchPredictionResultLocations = results,
            ParallelRunId = request.ParallelRunId,
            BatchSequenceNumber = request.BatchSequenceNumber,
        };

        await resultPublisher.PublishWithSession(batchResult, sessionId: request.SearchRequestId);

        logger.LogInformation(
            "Published batch result for search {SearchRequestId} (session) to parallel-match-prediction-results",
            request.SearchRequestId
        );
    }
}