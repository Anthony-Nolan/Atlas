namespace Atlas.MatchingAlgorithm.Settings;

/// <summary>
/// The donor genotype precomputation stage of the data refresh: the requests topic of its workers, the sweeps that the
/// data refresh function app runs over the batches, and the stage itself. Read from <c>DataRefresh:Precompute</c>.
/// </summary>
/// <remarks>
/// The settings that only the code reads have defaults here, like the lease settings of <see cref="DataRefreshSettings"/>,
/// so that an installation that has not had the app setting deployed yet still works.
/// </remarks>
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

    /// <summary>
    /// The groups of one batch: the work of one message for a worker. A run keeps the value that it was created with, so a
    /// build that starts again cuts the same batches.
    /// </summary>
    public int GroupsPerBatch { get; set; } = 1000;

    /// <summary>How often the stage reads the run while it waits for the workers.</summary>
    public int PollIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// How long the stage waits with no batch finished before it sends a stall alert. It sends one alert for each stall.
    /// It is also the longest time that the polls of the stage can fail with a temporary error. After that, the stage fails.
    /// </summary>
    public int StallAlertMinutes { get; set; } = 60;

    /// <summary>
    /// The largest fraction of the donors of a run, from 0 to 1, that can fail before the stage sends a high-priority
    /// alert. At or below it, the alert is of medium priority. 0 makes every failure high priority.
    /// </summary>
    /// <remarks>
    /// A failed donor has no stored genotype set for at least one combination of loci. The data refresh continues in both
    /// cases.
    /// </remarks>
    public double MaxFailedDonorFraction { get; set; } = 0.001;
}
