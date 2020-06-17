namespace Atlas.MatchingAlgorithm.Data.Persistent.Models
{
    /// <summary>
    /// The values of this enum are used to determine the order of a data refresh run.
    /// The values can be safely changed, but the order must remain the same. 
    /// </summary>
    public enum DataRefreshStageExecutionMode
    {
        /// <summary>
        /// "Special" mode for stages that aren't appropriate to be run in the atomic stage loop for any reason.
        /// </summary>
        NotApplicable = 0,
        /// <summary>
        /// Run the stage in question as though it's the first time this stage has been started in this Refresh.
        /// </summary>
        FromScratch = 1,
        /// <summary>
        /// Run this stage on the basis that it has previously been started, and you're now attempting to continue it from where it failed / was interrupted.
        /// </summary>
        Continuation = 2,
        /// <summary>
        /// Don't run this stage at all.
        /// </summary>
        Skip = 3
    }
}