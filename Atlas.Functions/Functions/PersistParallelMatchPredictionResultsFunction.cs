using System;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Functions.Services;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.SearchTracking.Data.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Atlas.Functions.Functions;

public class PersistParallelMatchPredictionResultsFunction
{
    private readonly ISearchRequestParallelMatchPredictionMetadataRepository metadataRepository;
    private readonly IParallelMatchPredictionCompletionService completionService;
    private readonly ILogger<PersistParallelMatchPredictionResultsFunction> logger;

    public PersistParallelMatchPredictionResultsFunction(
        ISearchRequestParallelMatchPredictionMetadataRepository metadataRepository,
        IParallelMatchPredictionCompletionService completionService,
        ILogger<PersistParallelMatchPredictionResultsFunction> logger)
    {
        this.metadataRepository = metadataRepository;
        this.completionService = completionService;
        this.logger = logger;
    }

    /// <summary>
    /// Session-aware Service Bus trigger that accumulates batch results published by the ACA Worker.
    /// One session per <c>SearchRequestId</c> ensures exclusive processing per search.
    /// When all batches have been received, the merged result locations are forwarded to
    /// <see cref="IParallelMatchPredictionCompletionService"/> which performs the same persistence
    /// pipeline as the durable orchestrator path.
    /// </summary>
    [Function(nameof(PersistParallelMatchPredictionResults))]
    public async Task PersistParallelMatchPredictionResults(
        [ServiceBusTrigger(
            "%AtlasFunction:MessagingServiceBus:ParallelMatchPredictionResultsTopic%",
            "%AtlasFunction:MessagingServiceBus:ParallelMatchPredictionResultsSubscription%",
            Connection = "AtlasFunction:MessagingServiceBus:ConnectionString",
            IsSessionsEnabled = true
        )]
        ParallelMatchPredictionBatchResult message)
    {
        var processedCount = await metadataRepository.AddBatchResultAndIncrementCount(
            message.ParallelMetadataId,
            message.MatchPredictionResultLocations
        );

        var metadata = await metadataRepository.Find(message.ParallelMetadataId);

        if (metadata == null)
        {
            throw new InvalidOperationException(
                $"No parallel match prediction metadata found with id '{message.ParallelMetadataId}'."
            );
        }

        if (processedCount < metadata.TotalBatchCount)
        {
            logger.LogInformation(
                "Received {ProcessedCount} of {TotalCount} batches processed for search {SearchRequestId}.",
                processedCount,
                metadata.TotalBatchCount,
                message.SearchIdentifier
            );
            return;
        }

        if (processedCount > metadata.TotalBatchCount)
        {
            logger.LogWarning(
                "Received more batches than expected for search {SearchRequestId}: {ProcessedCount} of {TotalCount}.",
                message.SearchIdentifier,
                processedCount,
                metadata.TotalBatchCount
            );
        }

        var locations = await metadataRepository.GetWithLocations(message.ParallelMetadataId);

        // All batches received – merge result locations and trigger persistence.
        var mergedLocations = locations.ResultLocations.ToDictionary(kv => kv.DonorId, kv => kv.ResultBlobFileName);

        logger.LogInformation(
            "All {Total} batches received for search {SearchRequestId}. Starting final persistence.",
            metadata.TotalBatchCount,
            message.SearchIdentifier
        );

        await completionService.Complete(metadata, mergedLocations);
    }
}