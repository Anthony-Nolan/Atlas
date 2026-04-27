using Atlas.Common.ServiceBus.BatchReceiving;
using Atlas.MatchPrediction.Exceptions;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.Worker.Settings;
using Microsoft.Extensions.Options;

namespace Atlas.MatchPrediction.Worker;

public class MatchPredictionWorker(
    IServiceScopeFactory serviceScopeFactory,
    IServiceBusMessageReceiver<IdentifiedMatchPredictionRequest> messageReceiver,
    IOptions<MatchPredictionWorkerSettings> settings,
    ILogger<MatchPredictionWorker> logger) : BackgroundService
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

            using var batchLock = new MessageBatchLock<IdentifiedMatchPredictionRequest>(messageReceiver, messages);

            try
            {
                await using var scope = serviceScopeFactory.CreateAsyncScope();
                var runner = scope.ServiceProvider.GetRequiredService<IMatchPredictionRequestRunner>();
                var requests = messages.Select(m => m.DeserializedBody).ToList();

                await runner.RunMatchPredictionRequestBatch(requests);
                await batchLock.CompleteBatchAsync();
            }
            catch (MatchPredictionRequestException ex)
            {
                logger.LogError(ex, "Unhandled error processing match prediction batch — abandoning batch.");
                await batchLock.AbandonBatchAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error processing match prediction batch — abandoning batch.");
                await batchLock.AbandonBatchAsync();
            }
        }

        logger.LogInformation("MatchPredictionWorker stopping.");
    }
}