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
        try
        {
            await ProcessBatch(request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error processing batch {BatchSequenceNumber} for run {ParallelRunId}, search {SearchRequestId}. " +
                "Publishing failure result so the batch is recorded as Failed.",
                request.BatchSequenceNumber, request.ParallelRunId, request.SearchRequestId
            );

            var failureResult = new ParallelMatchPredictionBatchResult
            {
                SearchIdentifier = new Guid(request.SearchRequestId),
                RepeatSearchIdentifier = request.RepeatSearchRequestId == null ? null : new Guid(request.RepeatSearchRequestId),
                ParallelRunId = request.ParallelRunId,
                BatchId = request.BatchId,
                BatchSequenceNumber = request.BatchSequenceNumber,
                IsSuccessful = false,
                FailureMessage = ex.Message,
                FailureException = ex.ToString(),
            };

            // Publish the failure result so the aggregator can record it and the run can still be finalised.
            // If publishing itself throws the exception propagates to the outer worker loop, which abandons the message for retry.
            await resultPublisher.PublishWithSession(failureResult, sessionId: request.SearchRequestId);

            logger.LogInformation(
                "Published failure result for batch {BatchSequenceNumber}, run {ParallelRunId}, search {SearchRequestId}.",
                request.BatchSequenceNumber, request.ParallelRunId, request.SearchRequestId
            );
        }
    }

    private async Task ProcessBatch(ParallelMatchPredictionBatchRequest request)
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

        var resultLocation = await parallelMatchPredictionAlgorithm.RunBatch(batchInput, maxDegreeOfParallelism, request.BatchId);

        await trackingDispatcher.ProcessRunningBatchesEnded(searchIdentifier, originalSearchIdentifier);

        logger.LogInformation(
            "Completed match prediction for batch {BatchId} ({DonorCount} donors) for search {SearchRequestId}; stored results in single blob {ResultLocation}",
            request.BatchId, batchInput.Donors?.Count, request.SearchRequestId, resultLocation
        );

        var batchResult = new ParallelMatchPredictionBatchResult
        {
            SearchIdentifier = new Guid(request.SearchRequestId),
            RepeatSearchIdentifier = request.RepeatSearchRequestId == null ? null : new Guid(request.RepeatSearchRequestId),
            IsSuccessful = true,
            MatchPredictionResultLocation = resultLocation,
            ParallelRunId = request.ParallelRunId,
            BatchId = request.BatchId,
            BatchSequenceNumber = request.BatchSequenceNumber,
        };

        await resultPublisher.PublishWithSession(batchResult, sessionId: request.SearchRequestId);

        logger.LogInformation(
            "Published batch result for batch {BatchId}, search {SearchRequestId} (session) to parallel-match-prediction-results",
            request.BatchId, request.SearchRequestId
        );
    }
}