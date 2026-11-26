namespace Atlas.RepeatSearch.Settings;

public class RepeatSearchSettings
{
    /// <summary>
    /// When the result set of the original search is not stored yet, the repeat search request is published again with this delay,
    /// to check again later.
    /// </summary>
    public int OriginalResultsWaitIntervalSeconds { get; set; }

    /// <summary>
    /// How long a repeat search waits for the result set of the original search to be stored, from the time of its first request.
    /// After this time, the repeat search fails with a validation error.
    /// </summary>
    public int MaxWaitForOriginalResultsMinutes { get; set; }

    /// <summary>
    /// The retention job deletes the result sets of original searches that have not been used for this number of days: no store and no
    /// repeat search. 0 or less turns the job off, which is the default: a delete cannot be undone, so each installation turns it on
    /// when it has agreed its retention. Keep it longer than any repeat search can run.
    /// The job runs on the schedule of the setting RepeatSearch:ResultsDeleteCronSchedule.
    /// </summary>
    public int ResultsRetentionDays { get; set; }

    /// <summary>
    /// Number of donor codes that the retention job deletes in one statement. Keep it below 5,000: at 5,000 locks in one statement,
    /// SQL Server tries to lock the whole table, which would stop the stores and repeat searches that write to it.
    /// </summary>
    public int ResultsDeleteBatchSize { get; set; }

    /// <summary>
    /// The retention job stops after this number of minutes, at the end of a batch. The next run continues the set that it was deleting.
    /// </summary>
    public int ResultsDeleteMaxRunMinutes { get; set; }
}
