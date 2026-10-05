#nullable enable

using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;

namespace Atlas.MatchingAlgorithm.Functions.DataRefresh.Functions;

/// <summary>
/// The timers and the dead-letter trigger of the donor genotype precomputation stage of the data refresh. The stage
/// builds the batches, sends them and waits, and the workers compute them. These functions make the moves that neither
/// of the two can make: they complete the run, notice lost leases and dead letters, and send failed batches again. The
/// logic is in <see cref="IDonorGenotypePrecomputationSweeper"/>.
/// </summary>
public class DonorGenotypePrecomputationFunctions
{
    private readonly IDonorGenotypePrecomputationSweeper sweeper;

    public DonorGenotypePrecomputationFunctions(IDonorGenotypePrecomputationSweeper sweeper)
    {
        this.sweeper = sweeper;
    }

    /// <summary>Completes a run whose batches are all done. The stage waits for this before the refresh goes on.</summary>
    [Function(nameof(FinaliseCompletedPrecomputationRuns))]
    public async Task FinaliseCompletedPrecomputationRuns(
        [TimerTrigger("%DataRefresh:Precompute:FinaliseRunsCronSchedule%")] TimerInfo _)
    {
        await sweeper.FinaliseCompletedRuns();
    }

    /// <summary>Marks the batches whose worker stopped without a result, so that the requeue timer sends them again.</summary>
    [Function(nameof(MarkAbandonedPrecomputationBatches))]
    public async Task MarkAbandonedPrecomputationBatches(
        [TimerTrigger("%DataRefresh:Precompute:AbandonBatchesCronSchedule%")] TimerInfo _)
    {
        await sweeper.MarkAbandonedBatches();
    }

    /// <summary>Sends failed and abandoned batches again, or gives up on the ones that have no retries left.</summary>
    [Function(nameof(RequeueFailedPrecomputationBatches))]
    public async Task RequeueFailedPrecomputationBatches(
        [TimerTrigger("%DataRefresh:Precompute:RequeueBatchesCronSchedule%")] TimerInfo _)
    {
        await sweeper.RequeueFailedBatches();
    }

    /// <summary>
    /// Abandons the batch of each dead-lettered batch message. Without this, the batch would stay requested with no
    /// message, and the stage would wait for it forever. Bound to the whole message, not to its body: the sweeper records
    /// the dead-letter reason and description on the batch row.
    /// </summary>
    [Function(nameof(AbandonDeadLetteredPrecomputationBatches))]
    public async Task AbandonDeadLetteredPrecomputationBatches(
        [ServiceBusTrigger(
            "%DataRefresh:Precompute:RequestsTopic%/Subscriptions/%DataRefresh:Precompute:RequestsSubscription%/$DeadLetterQueue",
            "%DataRefresh:Precompute:RequestsSubscription%",
            Connection = "MessagingServiceBus:ConnectionString")]
        ServiceBusReceivedMessage message)
    {
        await sweeper.AbandonDeadLetteredBatch(message);
    }
}
