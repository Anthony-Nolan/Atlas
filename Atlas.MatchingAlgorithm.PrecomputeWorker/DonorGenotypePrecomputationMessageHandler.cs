using System.Diagnostics;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using Azure.Messaging.ServiceBus;

namespace Atlas.MatchingAlgorithm.PrecomputeWorker;

/// <summary>What the worker does with a message after its batch.</summary>
internal enum MessageSettlement
{
    Complete,
    Abandon,
    DeadLetter
}

internal sealed record MessageDecision(MessageSettlement Settlement, string? DeadLetterReason = null, string? DeadLetterDescription = null)
{
    public static readonly MessageDecision Complete = new(MessageSettlement.Complete);

    public static readonly MessageDecision Abandon = new(MessageSettlement.Abandon);

    public static MessageDecision DeadLetter(string reason, string description) => new(MessageSettlement.DeadLetter, reason, description);
}

/// <summary>
/// Handles one message of the requests subscription: reads the database of the data refresh record that the message
/// names, and processes the batch of the message in that database, in a scope of its own.
/// </summary>
/// <remarks>
/// <para>
/// <b>Complete</b> when the batch is done with, in every way that
/// <see cref="IDonorGenotypePrecomputationBatchProcessor.ProcessBatch"/> returns: a failed batch is on its batch row, and
/// the requeue sweep of the data refresh sends it again.
/// </para>
///
/// <para>
/// <b>Complete, as skipped,</b> when the data refresh record of the message is not open: the refresh has ended or was
/// cancelled, so the batch is not the worker's to take. A redelivery would find the same, and the dead-letter trigger
/// also leaves the batch of a record that is not open.
/// </para>
///
/// <para>
/// <b>Abandon</b> when the record could not be read, or the batch could not be finished, for example because a database
/// failed: Service Bus delivers the message again, and after its last delivery dead-letters it.
/// </para>
///
/// <para>
/// <b>Dead-letter</b> when the body is not a batch request. Every delivery reads the same body, so a retry cannot help.
/// A body like this comes from a fault in the code that sent it, or from a message that someone sent by mistake, and the
/// worker cannot correct either. If the message was for a batch, that batch stays requested until someone abandons it by
/// hand.
/// </para>
/// </remarks>
internal class DonorGenotypePrecomputationMessageHandler(
    IServiceScopeFactory serviceScopeFactory,
    IDonorGenotypePrecomputationMetrics metrics,
    ILogger<DonorGenotypePrecomputationMessageHandler> logger)
{
    internal const string UnreadableBodyReason = "UnreadableBody";

    public async Task<MessageDecision> Handle(ServiceBusReceivedMessage message)
    {
        var request = DonorGenotypePrecomputationBatchRequest.FromBody(message.Body.ToString());
        if (request is null)
        {
            logger.LogError("Message {MessageId} is dead-lettered: its body is not a donor genotype precomputation batch request.",
                message.MessageId);
            return MessageDecision.DeadLetter(UnreadableBodyReason, "The body is not a donor genotype precomputation batch request.");
        }

        // The first delivery has a count of 1. A higher count means that the worker before stopped without a result.
        var isRedelivery = message.DeliveryCount > 1;

        try
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();

            var startTimestamp = Stopwatch.GetTimestamp();
            var database = await DonorGenotypePrecomputationTargetReader.ReadDatabase(
                scope.ServiceProvider.GetRequiredService<IDataRefreshHistoryRepository>(),
                request.DataRefreshRecordId);

            if (database is null)
            {
                // Debug, like a batch that the claim skips: after a failed refresh, every message that is left comes here.
                // The metric counts them.
                logger.LogDebug(
                    "Batch {BatchId} of run {RunId} was skipped: its data refresh record {DataRefreshRecordId} is not open.",
                    request.BatchId, request.RunId, request.DataRefreshRecordId);
                metrics.RecordBatch(DonorGenotypePrecomputationBatchResult.Skipped, Stopwatch.GetElapsedTime(startTimestamp), 0, 0);
                return MessageDecision.Complete;
            }

            var processor = scope.ServiceProvider.GetRequiredService<IDonorGenotypePrecomputationBatchProcessor>();
            await processor.ProcessBatch(request, database.Value, isRedelivery);

            return MessageDecision.Complete;
        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "Batch {BatchId} of run {RunId} could not be finished. The message is abandoned, so that Service Bus delivers it again.",
                request.BatchId, request.RunId);
            return MessageDecision.Abandon;
        }
    }
}
