namespace Atlas.Functions.Settings;

public class OrchestrationSettings
{
    public int MatchPredictionBatchSize { get; set; }

    /// <summary>Donor batch size used when preparing blobs for the parallel ACA Worker match-prediction path.</summary>
    public int ParallelMatchPredictionBatchSize { get; set; }

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
}