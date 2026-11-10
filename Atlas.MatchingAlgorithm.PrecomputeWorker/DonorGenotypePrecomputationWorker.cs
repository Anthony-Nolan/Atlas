using Azure.Messaging.ServiceBus;

namespace Atlas.MatchingAlgorithm.PrecomputeWorker;

/// <summary>
/// Reads the batch messages of the donor genotype precomputation stage from the requests subscription, and settles each
/// message after its batch. The work is in <see cref="DonorGenotypePrecomputationMessageHandler"/>.
/// </summary>
/// <remarks>
/// <para>
/// Before it takes its first message, it warms the HLA Metadata Dictionary tables that imputation reads
/// (<see cref="IDonorGenotypePrecomputationCacheWarmer"/>). The warm-up reads the open data refresh records and the active
/// haplotype frequency sets. It is best effort: when it fails, the worker starts with cold tables.
/// </para>
///
/// <para>
/// The handler reads the data refresh record of each message. Nothing reads a database when the host is created. So the
/// host can start outside a data refresh: the warm-up then warms nothing, and the worker skips every message.
/// </para>
/// </remarks>
internal class DonorGenotypePrecomputationWorker(
    ServiceBusProcessor processor,
    DonorGenotypePrecomputationMessageHandler handler,
    IDonorGenotypePrecomputationCacheWarmer cacheWarmer,
    ILogger<DonorGenotypePrecomputationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("DonorGenotypePrecomputationWorker starting.");

        // Before the warm-up, so that StopAsync can remove them also when the host stops during the warm-up.
        processor.ProcessMessageAsync += ProcessMessageAsync;
        processor.ProcessErrorAsync += ProcessErrorAsync;

        // Before the first message, so that the batches read the dictionary from memory. A stop does not wait for the
        // tables to load.
        await cacheWarmer.WarmUp().WaitAsync(stoppingToken);

        await processor.StartProcessingAsync(stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("DonorGenotypePrecomputationWorker stopping.");

        // First end ExecuteAsync. Then a warm-up that ends during the stop cannot start the processor after it has stopped.
        await base.StopAsync(cancellationToken);

        await processor.StopProcessingAsync(cancellationToken);

        processor.ProcessMessageAsync -= ProcessMessageAsync;
        processor.ProcessErrorAsync -= ProcessErrorAsync;
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
