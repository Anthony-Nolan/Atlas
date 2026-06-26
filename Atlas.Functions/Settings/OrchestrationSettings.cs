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
    /// CRON schedule for the parallel-MPA abandonment timer trigger (<c>MarkRunsAsAbandoned</c>).
    /// Six-field NCrontab format.
    /// </summary>
    public string ParallelBatchAbandonmentCronSchedule { get; set; }

    /// <summary>
    /// Number of days to retain per-batch rows after a parallel MPA run finalises. Parent run rows
    /// are always retained — only the verbose per-batch rows are purged. Also reused as the retention
    /// period for abandoned runs.
    /// </summary>
    public int ParallelBatchRetentionDays { get; set; }

    /// <summary>
    /// Minutes after a parallel MPA run was initiated (<c>MatchPredictionRunInitiatedUtc</c>) before a run that
    /// still has un-returned batches is abandoned. Used by the <c>MarkRunsAsAbandoned</c> timer to stop waiting
    /// for lost batches.
    /// </summary>
    public int AbandonBatchAfterMinutes { get; set; }
}