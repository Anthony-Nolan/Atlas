using System.ComponentModel.DataAnnotations;

namespace Atlas.MatchingAlgorithm.Settings;

/// <summary>
/// The donor genotype precomputation worker: where it reads its batch messages, how many it takes at a time, and how it
/// holds and finishes a batch. Read from <c>PrecomputeWorker</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>A batch must finish within <see cref="BatchLeaseMinutes"/> and within <see cref="MaxAutoLockRenewalMinutes"/>.</b>
/// After the lease, the sweeps give the batch to another worker. After the lock renewal, Service Bus delivers the message
/// again, and the redelivery takes the batch. In both cases the work of the first worker is lost, and the batch is
/// computed twice.
/// </para>
///
/// <para>
/// <b>Database limits:</b> <see cref="MaxConcurrentCalls"/> times the replica count must stay under the worker and session
/// limits of the dormant database.
/// </para>
/// </remarks>
public class DonorGenotypePrecomputationWorkerSettings
{
    [Required(AllowEmptyStrings = false)]
    public string RequestsTopic { get; set; }

    [Required(AllowEmptyStrings = false)]
    public string RequestsSubscription { get; set; }

    /// <summary>The batches that one replica processes at a time. Each one computes its values one after the other.</summary>
    [Range(1, int.MaxValue)]
    public int MaxConcurrentCalls { get; set; } = 1;

    /// <summary>Messages that one replica fetches before it needs them. 0 (the default) leaves them for the other replicas.</summary>
    [Range(0, int.MaxValue)]
    public int PrefetchCount { get; set; }

    /// <summary>How long the worker renews the lock of a message while it processes the batch.</summary>
    [Range(1, int.MaxValue)]
    public int MaxAutoLockRenewalMinutes { get; set; } = 60;

    /// <summary>How long the claim of a batch holds before a sweep can mark the batch abandoned.</summary>
    [Range(1, int.MaxValue)]
    public int BatchLeaseMinutes { get; set; } = 60;

    /// <summary>
    /// The permanent group failures, such as bad typings, that one batch can have and still succeed. More than this is a
    /// sign that something else is wrong, such as HLA Metadata Dictionary data that is missing, so the batch fails and is
    /// sent again. Keep it well below the <c>GroupsPerBatch</c> of a run.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int MaxGroupFailuresPerBatch { get; set; } = 100;
}
