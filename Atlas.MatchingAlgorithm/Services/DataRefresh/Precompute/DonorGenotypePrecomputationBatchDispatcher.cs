#nullable enable

using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ServiceBus;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Microsoft.Extensions.Logging;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;

public interface IDonorGenotypePrecomputationBatchDispatcher
{
    /// <summary>
    /// Publishes one message for each <see cref="Data.Models.Entities.DonorGenotypePrecomputationBatchStatus.Pending"/>
    /// batch of the run that <paramref name="selection"/> takes, then moves those batches to
    /// <see cref="Data.Models.Entities.DonorGenotypePrecomputationBatchStatus.Requested"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The message first, then the status.</b> A stop between the two leaves the batches pending, and the next dispatch
    /// sends them again. A second message for a batch does no harm: its claim finds the batch taken or done. The other
    /// order would leave a requested batch with no message, and the stage would wait for it forever.
    /// </para>
    ///
    /// <para>
    /// <b>The stage and the requeue sweep both dispatch, and they can overlap.</b> Between the publish and the status update,
    /// a worker can fail a batch and the sweep can send it back. The status update then leaves that batch to the sweep (see
    /// <see cref="Data.Repositories.Precompute.IDonorGenotypePrecomputationRepository.MarkBatchesRequested"/>).
    /// </para>
    ///
    /// <para>
    /// A chunk at a time, in id order, so a dispatch of all the batches of a full run holds one chunk of messages in
    /// memory, and a failed dispatch has sent a prefix of the batches.
    /// </para>
    /// </remarks>
    /// <returns>The number of messages published.</returns>
    Task<int> DispatchPendingBatches(DonorGenotypePrecomputationRunLocation run, PendingBatchSelection selection);
}

internal class DonorGenotypePrecomputationBatchDispatcher : IDonorGenotypePrecomputationBatchDispatcher
{
    private const string LoggingPrefix = "DONOR GENOTYPE PRECOMPUTATION:";

    /// <summary>Batches per read, publish and status update.</summary>
    internal const int ChunkSize = 1000;

    private readonly IStaticallyChosenDatabaseRepositoryFactory repositoryFactory;
    private readonly IMessageBatchPublisher<DonorGenotypePrecomputationBatchRequest> publisher;
    private readonly ILogger<DonorGenotypePrecomputationBatchDispatcher> logger;

    public DonorGenotypePrecomputationBatchDispatcher(
        IStaticallyChosenDatabaseRepositoryFactory repositoryFactory,
        IMessageBatchPublisher<DonorGenotypePrecomputationBatchRequest> publisher,
        ILogger<DonorGenotypePrecomputationBatchDispatcher> logger)
    {
        this.repositoryFactory = repositoryFactory;
        this.publisher = publisher;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<int> DispatchPendingBatches(DonorGenotypePrecomputationRunLocation run, PendingBatchSelection selection)
    {
        var repository = repositoryFactory.GetDonorGenotypePrecomputationRepositoryForDatabase(run.TargetDatabase);

        var publishedCount = 0;
        var afterBatchId = 0;
        while (true)
        {
            var batches = await repository.GetPendingBatches(run.RunId, selection, afterBatchId, ChunkSize);
            if (batches.Count == 0)
            {
                break;
            }

            await publisher.BatchPublish(batches.Select(batch => new DonorGenotypePrecomputationBatchRequest
            {
                DataRefreshRecordId = run.DataRefreshRecordId,
                RunId = run.RunId,
                BatchId = batch.BatchId
            }));
            await repository.MarkBatchesRequested(run.RunId, batches);

            publishedCount += batches.Count;
            afterBatchId = batches[^1].BatchId;
        }

        if (publishedCount > 0)
        {
            logger.LogInformation(
                LoggingPrefix + " Dispatched {BatchCount} batch(es) of run {RunId} (data refresh record {DataRefreshRecordId}, {TargetDatabase}). " +
                "Selection: {PendingBatchSelection}.",
                publishedCount, run.RunId, run.DataRefreshRecordId, run.TargetDatabase, selection);
        }

        return publishedCount;
    }
}
