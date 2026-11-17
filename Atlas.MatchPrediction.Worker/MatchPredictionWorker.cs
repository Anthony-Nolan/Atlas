using System.Text;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.Worker.Services;
using Atlas.MatchPrediction.Worker.Settings;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Atlas.MatchPrediction.Worker;

public class MatchPredictionWorker(
    ServiceBusProcessor processor,
    IServiceScopeFactory serviceScopeFactory,
    IOptions<MatchPredictionWorkerSettings> settings,
    ILogger<MatchPredictionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("MatchPredictionWorker starting.");

        processor.ProcessMessageAsync += ProcessMessageAsync;
        processor.ProcessErrorAsync += ProcessErrorAsync;

        await processor.StartProcessingAsync(stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("MatchPredictionWorker stopping.");

        await processor.StopProcessingAsync(cancellationToken);

        processor.ProcessMessageAsync -= ProcessMessageAsync;
        processor.ProcessErrorAsync -= ProcessErrorAsync;

        await base.StopAsync(cancellationToken);
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        // Service Bus counts this delivery too. After the last one it dead-letters the message instead of delivering it again.
        var deliveryCount = args.Message.DeliveryCount;
        var maxDeliveryCount = settings.Value.RequestsMaxDeliveryCount;
        var isLastDelivery = deliveryCount >= maxDeliveryCount;
        ParallelMatchPredictionBatchRequest request = null;

        try
        {
            // we don't expect a malformed body: every message on this topic is produced by our own
            // orchestrator, and nothing writes to the queue by hand. Deserializing inside the try is defensive,
            // should a bad body ever appear it is abandoned (and eventually dead-lettered once the
            // subscription's max delivery count is exceeded) rather than escaping uncaught.
            request = JsonConvert.DeserializeObject<ParallelMatchPredictionBatchRequest>(
                Encoding.UTF8.GetString(args.Message.Body)
            );

            if (request is null)
            {
                throw new InvalidOperationException("Parallel match prediction batch request deserialized to null.");
            }

            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var runner = scope.ServiceProvider.GetRequiredService<IParallelMatchPredictionBatchRunner>();

            await runner.RunBatch(request, isLastDelivery);

            await args.CompleteMessageAsync(args.Message, args.CancellationToken);
        }
        catch (Exception ex)
        {
            // The one log of a failed delivery. A Warning while retries are left, because the batch can still work;
            // an Error on the last delivery, because the batch is dead-lettered. ATL-546.
            logger.Log(isLastDelivery ? LogLevel.Error : LogLevel.Warning, ex,
                "Error processing parallel match prediction batch {BatchSequenceNumber} for run {ParallelRunId}, " +
                "search {SearchRequestId} on delivery {DeliveryCount} of {MaxDeliveryCount}. Abandoning the message: " +
                "Service Bus delivers it again, or dead-letters it after the last delivery.",
                request?.BatchSequenceNumber, request?.ParallelRunId, request?.SearchRequestId, deliveryCount, maxDeliveryCount);
            await args.AbandonMessageAsync(args.Message, cancellationToken: args.CancellationToken);
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        logger.LogError(
            args.Exception,
            "Service Bus processor error. Source: {ErrorSource}, Entity: {EntityPath}.",
            args.ErrorSource, args.EntityPath
        );
        return Task.CompletedTask;
    }
}