using Atlas.Common.AzureStorage.Blob;
using Atlas.Common.ServiceBus;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MatchPrediction.Worker.Settings;
using Atlas.SearchTracking.Common.Dispatchers;
using Microsoft.Extensions.Options;

namespace Atlas.MatchPrediction.Worker.Services;

public interface IParallelMatchPredictionBatchRunner
{
    /// <param name="request">The batch to run.</param>
    /// <param name="deliveryCount">How many times Service Bus has delivered this message, including this delivery.</param>
    Task RunBatch(ParallelMatchPredictionBatchRequest request, int deliveryCount);
}

internal class ParallelMatchPredictionBatchRunner : IParallelMatchPredictionBatchRunner
{
    private readonly IBlobDownloader blobDownloader;
    private readonly IParallelMatchPredictionAlgorithm parallelMatchPredictionAlgorithm;
    private readonly ISessionMessagePublisher<ParallelMatchPredictionBatchResult> resultPublisher;
    private readonly IMatchPredictionSearchTrackingDispatcher trackingDispatcher;
    private readonly string requestsContainer;
    private readonly int maxDegreeOfParallelism;
    private readonly int maxDeliveryCount;
    private readonly ILogger<ParallelMatchPredictionBatchRunner> logger;

    public ParallelMatchPredictionBatchRunner(
        IBlobDownloader blobDownloader,
        IParallelMatchPredictionAlgorithm parallelMatchPredictionAlgorithm,
        ISessionMessagePublisher<ParallelMatchPredictionBatchResult> resultPublisher,
        IMatchPredictionSearchTrackingDispatcher trackingDispatcher,
        IOptions<AzureStorageSettings> azureStorageSettings,
        IOptions<MatchPredictionRequestsSettings> matchPredictionRequestsSettings,
        IOptions<MatchPredictionWorkerSettings> workerSettings,
        ILogger<ParallelMatchPredictionBatchRunner> logger)
    {
        this.blobDownloader = blobDownloader;
        this.parallelMatchPredictionAlgorithm = parallelMatchPredictionAlgorithm;
        this.resultPublisher = resultPublisher;
        this.trackingDispatcher = trackingDispatcher;
        requestsContainer = azureStorageSettings.Value.MatchPredictionRequestsBlobContainer;
        maxDegreeOfParallelism = matchPredictionRequestsSettings.Value.MaxParallelism;
        maxDeliveryCount = workerSettings.Value.RequestsMaxDeliveryCount;
        this.logger = logger;
    }

    public async Task RunBatch(ParallelMatchPredictionBatchRequest request, int deliveryCount)
    {
        // 1. Compute the batch. Only an error here is a batch failure.
        ParallelMatchPredictionBatchOutput batchOutput;
        try
        {
            batchOutput = await ComputeBatch(request);
        }
        catch (Exception ex) when (deliveryCount < maxDeliveryCount)
        {
            // Retries are left: report nothing, so the run waits for the retry and the search gets one notification.
            // Rethrow so the worker abandons the message and Service Bus delivers it again. See ATL-546.
            logger.LogWarning(ex,
                "Error processing batch {BatchSequenceNumber} for run {ParallelRunId}, search {SearchRequestId} " +
                "on delivery {DeliveryCount} of {MaxDeliveryCount}. The batch will be retried.",
                request.BatchSequenceNumber, request.ParallelRunId, request.SearchRequestId, deliveryCount, maxDeliveryCount
            );
            throw;
        }
        catch (Exception ex)
        {
            // Last delivery: no retries are left, so report the batch as failed and let the run be finalised as failed.
            logger.LogError(ex,
                "Error processing batch {BatchSequenceNumber} for run {ParallelRunId}, search {SearchRequestId} " +
                "on the last delivery ({DeliveryCount} of {MaxDeliveryCount}). Publishing failure result so the batch is recorded as Failed.",
                request.BatchSequenceNumber, request.ParallelRunId, request.SearchRequestId, deliveryCount, maxDeliveryCount
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

            // Publish the failure result so the aggregator records the batch as Failed and the run can still be finalised.
            // An earlier delivery that lost its message lock may still be running; if it succeeds later, the aggregator
            // keeps this failure, because the first outcome of a batch stays (ATL-546).
            // If this publish fails, the message is dead-lettered with the batch still Requested, and the abandonment
            // sweep fails the run after AbandonBatchAfterMinutes.
            await resultPublisher.PublishWithSession(failureResult, sessionId: request.SearchRequestId);

            logger.LogInformation(
                "Published failure result for batch {BatchSequenceNumber}, run {ParallelRunId}, search {SearchRequestId}.",
                request.BatchSequenceNumber, request.ParallelRunId, request.SearchRequestId
            );
            
            // Rethrow so the worker abandons the message instead of completing it: on the last delivery Service Bus
            // moves it to the dead-letter queue, where it can be replayed by hand. See ATL-251, ATL-148.
            throw;
        }

        // 2. Send the result. Outside the catch above: the result exists, so an error from here on is not a batch
        // failure. It is rethrown, and Service Bus delivers the batch again. After the last delivery nothing records
        // the batch, and the abandonment sweep fails the run after AbandonBatchAfterMinutes. ATL-546.
        await PublishSuccess(request, batchOutput);
    }

    /// <summary>Downloads the batch input and runs match prediction on it; the results are stored in a blob.</summary>
    private async Task<ParallelMatchPredictionBatchOutput> ComputeBatch(ParallelMatchPredictionBatchRequest request)
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

        var (searchIdentifier, originalSearchIdentifier) = TrackingIdentifiers(request);
        await trackingDispatcher.ProcessRunningBatchesStarted(searchIdentifier, originalSearchIdentifier);

        var batchOutput = await parallelMatchPredictionAlgorithm.RunBatch(batchInput, maxDegreeOfParallelism, request.BatchId);

        logger.LogInformation(
            "Completed match prediction for batch {BatchId} ({DonorCount} donors) for search {SearchRequestId}; stored results in single blob {ResultLocation}",
            request.BatchId, batchInput.Donors?.Count, request.SearchRequestId, batchOutput.ResultLocation
        );

        return batchOutput;
    }

    /// <summary>Tells search tracking that the batch has ended, and publishes its result for the aggregator.</summary>
    private async Task PublishSuccess(ParallelMatchPredictionBatchRequest request, ParallelMatchPredictionBatchOutput batchOutput)
    {
        var (searchIdentifier, originalSearchIdentifier) = TrackingIdentifiers(request);
        await trackingDispatcher.ProcessRunningBatchesEnded(searchIdentifier, originalSearchIdentifier);

        var batchResult = new ParallelMatchPredictionBatchResult
        {
            SearchIdentifier = new Guid(request.SearchRequestId),
            RepeatSearchIdentifier = request.RepeatSearchRequestId == null ? null : new Guid(request.RepeatSearchRequestId),
            IsSuccessful = true,
            MatchPredictionResultLocation = batchOutput.ResultLocation,
            ParallelRunId = request.ParallelRunId,
            BatchId = request.BatchId,
            BatchSequenceNumber = request.BatchSequenceNumber,
            PatientGenotypeCount = batchOutput.PatientGenotypeCount,
            DonorGenotypeCounts = new Dictionary<int, int>(batchOutput.DonorGenotypeCounts),
        };

        await resultPublisher.PublishWithSession(batchResult, sessionId: request.SearchRequestId);

        logger.LogInformation(
            "Published batch result for batch {BatchId}, search {SearchRequestId} (session) to parallel-match-prediction-results",
            request.BatchId, request.SearchRequestId
        );
    }

    /// <summary>Search tracking keys a repeat search on its own id, with the original search as the second id.</summary>
    private static (Guid SearchIdentifier, Guid? OriginalSearchIdentifier) TrackingIdentifiers(ParallelMatchPredictionBatchRequest request) =>
        request.IsRepeatSearch
            ? (Guid.Parse(request.RepeatSearchRequestId), Guid.Parse(request.SearchRequestId))
            : (Guid.Parse(request.SearchRequestId), null);
}