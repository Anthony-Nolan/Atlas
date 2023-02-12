using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.Matching;

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
        /// <inheritdoc cref="SearchFailureInfo.StageReached"/>
        /// </summary>
        public string StageReached { get; set; }

        /// <summary>
        /// <inheritdoc cref="Client.Models.Search.Results.Matching.MatchingAlgorithmFailureInfo"/>
        /// </summary>
        public MatchingAlgorithmFailureInfo MatchingAlgorithmFailureInfo { get; set; }
    }
}
