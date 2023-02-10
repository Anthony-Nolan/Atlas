namespace Atlas.Client.Models.Search.Results
{
    public class SearchFailureInfo
    {
        /// <summary>
        /// Stage of search at which point the failure occurred.
        /// </summary>
        public string StageReached { get; set; }

        /// <summary>
        /// Will only have a value in the event that the matching algorithm portion of the request failed due to a validation error.
        /// </summary>
        public string MatchingAlgorithmValidationError { get; set; }

        /// <summary>
        /// Number of times this request has been processed so far, including the current run.
        /// I.e., On the first attempt, <see cref="AttemptNumber"/> will have a value of 1.
        /// </summary>
        public int AttemptNumber { get; set; }

        /// <summary>
        /// Number of retries remaining: if the value is 0, this request will not be automatically re-run.
        /// </summary>
        public int RemainingRetriesCount { get; set; }
    }
}
