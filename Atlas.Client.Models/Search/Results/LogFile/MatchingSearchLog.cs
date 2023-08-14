namespace Atlas.Client.Models.Search.Results.LogFile
{
    /// <summary>
    /// Log for matching algorithm requests.
    /// </summary>
    public class MatchingSearchLog : SearchLog
    {
        /// <summary>
        /// How many times was the matching request replayed before it completed?
        /// </summary>
        public int AttemptNumber { get; set; }
    }
}
