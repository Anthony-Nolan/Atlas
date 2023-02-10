using Atlas.Client.Models.Search.Results;

namespace Atlas.Functions.Models
{
    /// <summary>
    /// Information required when sending out a failed search notification
    /// </summary>
    public class FailureNotificationRequestInfo
    {
        public string SearchRequestId { get; set; }
        public string RepeatSearchRequestId { get; set; }

        /// <summary>
        /// See <see cref="SearchFailureInfo.StageReached"/>
        /// </summary>
        public string StageReached { get; set; }

        /// <summary>
        /// See <see cref="SearchFailureInfo.AttemptNumber"/>
        /// </summary>
        public int AttemptNumber { get; set; }

        /// <summary>
        /// See <see cref="SearchFailureInfo.MatchingAlgorithmValidationError"/>
        /// </summary>
        public string MatchingAlgorithmValidationError { get; set; }

        /// <summary>
        /// Will be set to `true` in the event that a search has been deemed "permanently" failed, and there is no benefit in replaying it.
        /// E.g., a validation error.
        /// </summary>
        public bool WillNotBeRetried { get; set; }
    }
}
