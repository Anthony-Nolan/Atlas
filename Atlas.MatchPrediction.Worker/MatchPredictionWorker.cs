using Atlas.Common.Caching;
using Atlas.Common.ServiceBus.BatchReceiving;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.Worker.Services;
using Atlas.MatchPrediction.Worker.Settings;
using LazyCache.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Atlas.MatchPrediction.Worker;

public class MatchPredictionWorker(
    IServiceScopeFactory serviceScopeFactory,
    IServiceBusMessageReceiver<ParallelMatchPredictionBatchRequest> messageReceiver,
    IOptions<MatchPredictionWorkerSettings> settings,
    ILogger<MatchPredictionWorker> logger,
    IPersistentCacheProvider cacheProvider) : BackgroundService
{
    private readonly int batchSize = settings.Value.BatchSize;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("MatchPredictionWorker starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var messages = (await messageReceiver.ReceiveMessageBatchAsync(batchSize)).ToList();

            if (!messages.Any())
            {
                logger.LogInformation("No messages received, waiting for 5 seconds before retrying.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                continue;
            }

            using var batchLock = new MessageBatchLock<ParallelMatchPredictionBatchRequest>(messageReceiver, messages);

            try
            {
                await using var scope = serviceScopeFactory.CreateAsyncScope();
                var runner = scope.ServiceProvider.GetRequiredService<IParallelMatchPredictionBatchRunner>();
                var requests = messages.Select(m => m.DeserializedBody).ToList();

                foreach (var request in requests)
                {
                    try
                    {
                        await runner.RunBatch(request);
                    }
                    finally
                    {
                        cacheProvider.ClearCache();
                    }
                }

                await batchLock.CompleteBatchAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error processing parallel match prediction batch — abandoning batch.");
                await batchLock.AbandonBatchAsync();
            }
       }

        logger.LogInformation("MatchPredictionWorker stopping.");
    }
}