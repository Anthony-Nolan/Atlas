using Azure.Messaging.ServiceBus;

namespace Atlas.MatchingAlgorithm.PrecomputeWorker;

/// <summary>
/// Reads the batch messages of the donor genotype precomputation stage from the requests subscription, and settles each
/// message after its batch. The work is in <see cref="DonorGenotypePrecomputationMessageHandler"/>.
/// </summary>
/// <remarks>
/// It reads no database when it starts: the handler reads the data refresh record of each message. So the host can start
/// outside a data refresh, and before a release migrates the databases.
/// </remarks>
internal class DonorGenotypePrecomputationWorker(
    ServiceBusProcessor processor,
    DonorGenotypePrecomputationMessageHandler handler,
    ILogger<DonorGenotypePrecomputationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("DonorGenotypePrecomputationWorker starting.");

        processor.ProcessMessageAsync += ProcessMessageAsync;
        processor.ProcessErrorAsync += ProcessErrorAsync;

        await processor.StartProcessingAsync(stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("DonorGenotypePrecomputationWorker stopping.");

        await processor.StopProcessingAsync(cancellationToken);

        processor.ProcessMessageAsync -= ProcessMessageAsync;
        processor.ProcessErrorAsync -= ProcessErrorAsync;

        await base.StopAsync(cancellationToken);
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var decision = await handler.Handle(args.Message);

        try
        {
            switch (decision.Settlement)
            {
                case MessageSettlement.Complete:
                    await args.CompleteMessageAsync(args.Message, args.CancellationToken);
                    break;
                case MessageSettlement.DeadLetter:
                    await args.DeadLetterMessageAsync(args.Message, decision.DeadLetterReason, decision.DeadLetterDescription, args.CancellationToken);
                    break;
                default:
                    await args.AbandonMessageAsync(args.Message, cancellationToken: args.CancellationToken);
                    break;
            }
        }
        catch (Exception exception)
        {
            // For example a lost lock, after a batch that took longer than the lock renewal. Service Bus delivers the
            // message again, and the claim of the redelivery decides who owns the batch.
            logger.LogError(exception, "Message {MessageId} could not be settled ({Settlement}).", args.Message.MessageId, decision.Settlement);
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
