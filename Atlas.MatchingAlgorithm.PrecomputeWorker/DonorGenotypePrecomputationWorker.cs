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
/// haplotype frequency sets. It is best effort: when it fails, the worker starts with cold tables. The worker waits for it
/// for at most <see cref="WarmUpTimeLimit"/>: then the worker starts, and the warm-up continues in the background.
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
    TimeProvider timeProvider,
    ILogger<DonorGenotypePrecomputationWorker> logger) : BackgroundService
{
    /// <summary>
    /// The longest time that the worker waits for the warm-up before it takes messages.
    /// </summary>
    /// <remarks>
    /// The warm-up reads whole tables from Table Storage. When Table Storage is slow, for example when it throttles the many
    /// replicas that start at the same time, a replica would otherwise take no messages until the reads end. After this time,
    /// the replica takes messages, and the reads continue in the background. Until a table is loaded, each lookup in it reads
    /// a single row, as it does without the warm-up. The limit is longer than a normal warm-up, so the worker stops waiting
    /// only for a slow one.
    /// </remarks>
    internal static readonly TimeSpan WarmUpTimeLimit = TimeSpan.FromMinutes(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("DonorGenotypePrecomputationWorker starting.");

        // Before the warm-up, so that StopAsync can remove them also when the host stops during the warm-up.
        processor.ProcessMessageAsync += ProcessMessageAsync;
        processor.ProcessErrorAsync += ProcessErrorAsync;

        await WaitForWarmUp(stoppingToken);

        await processor.StartProcessingAsync(stoppingToken);
    }

    /// <summary>
    /// Waits for the warm-up before the first message, so that the batches read the dictionary from memory: for at most
    /// <see cref="WarmUpTimeLimit"/>. A stop does not wait for the tables to load. A warm-up that has not ended when the
    /// worker stops waiting continues in the background, and fills the cache when it ends.
    /// </summary>
    private async Task WaitForWarmUp(CancellationToken stoppingToken)
    {
        try
        {
            await cacheWarmer.WarmUp().WaitAsync(WarmUpTimeLimit, timeProvider, stoppingToken);
        }
        catch (TimeoutException)
        {
            logger.LogWarning(
                "The HLA Metadata Dictionary caches were not warmed within {WarmUpTimeLimitSeconds} s. The worker now takes " +
                "messages, and the tables continue to load in the background. Until a table is loaded, each lookup in it " +
                "reads a single row from Table Storage.",
                WarmUpTimeLimit.TotalSeconds);
        }
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
