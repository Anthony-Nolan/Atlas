namespace Atlas.Functions.Settings;

public class OrchestrationSettings
{
    public int MatchPredictionBatchSize { get; set; }

    /// <summary>Donor batch size used when preparing blobs for the parallel ACA Worker MPA path.</summary>
    public int ParallelMpaBatchSize { get; set; }

    /// <summary>
    /// CRON schedule for the parallel-MPA finaliser timer trigger
    /// (<c>FinaliseCompletedParallelMatchPredictionRuns</c>). Six-field NCrontab format.
    /// </summary>
    public string ParallelFinalisationCronSchedule { get; set; }

    /// <summary>
    /// CRON schedule for the parallel-MPA batch cleanup timer trigger
    /// (<c>CleanupOldParallelMatchPredictionBatches</c>). Six-field NCrontab format.
    /// </summary>
    public string ParallelBatchCleanupCronSchedule { get; set; }

    /// <summary>
    /// Number of days to retain per-batch rows after a parallel MPA run finalises. Parent run rows
    /// are always retained — only the verbose per-batch rows are purged.
    /// </summary>
    public int ParallelBatchRetentionDays { get; set; }

    /// <summary>
    /// Duration (in minutes) of the finalisation lease a finaliser takes on a completed parallel MPA run
    /// before performing the persistence pipeline. It must comfortably exceed the worst-case time to run
    /// that pipeline: if a finaliser crashes midway, the run only becomes eligible for re-Finalisation once
    /// this lease lapses.
    /// </summary>
    public int ParallelFinalisationLeaseDurationMinutes { get; set; }
}