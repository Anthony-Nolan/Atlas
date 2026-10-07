namespace Atlas.MatchingAlgorithm.Settings;

/// <summary>
/// The donor genotype precomputation stage of the data refresh: the requests topic of its workers, and the sweeps that
/// the data refresh function app runs over the batches. Read from <c>DataRefresh:Precompute</c>.
/// </summary>
public class DonorGenotypePrecomputationSettings
{
    /// <summary>The topic of the batch messages. The stage and the requeue sweep publish to it, and the workers read it.</summary>
    public string RequestsTopic { get; set; }

    /// <summary>
    /// The subscription of the workers to <see cref="RequestsTopic"/>. The data refresh app reads only its dead-letter
    /// queue.
    /// </summary>
    public string RequestsSubscription { get; set; }

    /// <summary>
    /// How many times a failed or abandoned batch is sent again before the requeue sweep gives up on it. 0 gives up at the
    /// first failure.
    /// </summary>
    /// <remarks>
    /// Defaulted here, like the lease settings of <see cref="DataRefreshSettings"/>, so that an installation that has not
    /// had the app setting deployed yet still retries.
    /// </remarks>
    public int MaxBatchRetries { get; set; } = 3;

    /// <summary>
    /// A crontab: how often the finaliser completes a run whose batches are all done. The stage waits for it, so this is
    /// also the longest time between the last batch and the next stage.
    /// </summary>
    /// <remarks>
    /// Only the TimerTrigger binding reads this, and the Functions host reads it to index the function. Without the app
    /// setting, only that one function fails to index: its sweep never runs, and the stage can wait forever.
    /// </remarks>
    public string FinaliseRunsCronSchedule { get; set; }

    /// <summary>A crontab: how often the abandon sweep finds the batches whose lease has expired.</summary>
    /// <inheritdoc cref="FinaliseRunsCronSchedule" path="/remarks"/>
    public string AbandonBatchesCronSchedule { get; set; }

    /// <summary>A crontab: how often the requeue sweep sends failed and abandoned batches again, or gives up on them.</summary>
    /// <inheritdoc cref="FinaliseRunsCronSchedule" path="/remarks"/>
    public string RequeueBatchesCronSchedule { get; set; }
}
