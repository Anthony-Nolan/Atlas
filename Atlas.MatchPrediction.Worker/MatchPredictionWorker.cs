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
        // We expect that message body is always a valid JSON
        var request = JsonConvert.DeserializeObject<ParallelMatchPredictionBatchRequest>(args.Message.Body.ToString());

        try
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var runner = scope.ServiceProvider.GetRequiredService<IParallelMatchPredictionBatchRunner>();

            await runner.RunBatch(request!);

            await args.CompleteMessageAsync(args.Message, args.CancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error processing parallel match prediction batch — abandoning message.");
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