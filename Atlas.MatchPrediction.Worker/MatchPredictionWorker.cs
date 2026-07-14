using System.Text;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.Worker.Services;
using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;

namespace Atlas.MatchPrediction.Worker;

public class MatchPredictionWorker(
    ServiceBusProcessor processor,
    IServiceScopeFactory serviceScopeFactory,
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
        try
        {
            // we don't expect a malformed body: every message on this topic is produced by our own
            // orchestrator, and nothing writes to the queue by hand. Deserializing inside the try is defensive,
            // should a bad body ever appear it is abandoned (and eventually dead-lettered once the
            // subscription's max delivery count is exceeded) rather than escaping uncaught.
            var request = JsonConvert.DeserializeObject<ParallelMatchPredictionBatchRequest>(
                Encoding.UTF8.GetString(args.Message.Body)
            );

            if (request is null)
            {
                throw new InvalidOperationException("Parallel match prediction batch request deserialized to null.");
            }

            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var runner = scope.ServiceProvider.GetRequiredService<IParallelMatchPredictionBatchRunner>();

            await runner.RunBatch(request);

            await args.CompleteMessageAsync(args.Message, args.CancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error processing parallel match prediction batch — abandoning message for redelivery/dead-lettering.");
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