using Atlas.Client.Models.Search.Results.Matching;

namespace Atlas.Client.Models.Search.Results
{
    /// <summary>
    /// Information related to search request failure.
    /// </summary>
    public class SearchFailureInfo
    {
        /// <summary>
        /// Stage of search at which point the failure occurred.
        /// </summary>
        public string StageReached { get; set; }

        /// <summary>
        /// <inheritdoc cref="Matching.MatchingAlgorithmFailureInfo"/>
        /// Will be empty if failure occurred at a different stage.
        /// </summary>
        public MatchingAlgorithmFailureInfo MatchingAlgorithmFailureInfo { get; set; }

        /// <summary>
        /// Will the search be retried?
        /// If `false`, then the search can be deemed as "permanently" failed; else, expect to receive another <see cref="SearchResultsNotification"/>.
        /// 
        /// At present, search will only be retried from the beginning if the failure occurred within the matching algorithm & there are retries remaining on matching request.
        /// Failures that occur within the Search Orchestrator are retried on the Activity Function level and not at the search request level, and so <see cref="WillRetry"/> will be `false`.
        /// Failures in match prediction batch processing are sent when the failed batch has no retries left, or when the run is abandoned because a batch had not returned
        /// within the configured time; <see cref="WillRetry"/> is `false` for both.
        /// Another notification can still follow such a failure in two cases: the run was abandoned and the batch returned later, which sends a success, or a second failure
        /// with the real cause; or support replayed a dead-lettered batch by hand.
        /// </summary>
        public bool WillRetry => MatchingAlgorithmFailureInfo is { RemainingRetriesCount: > 0 };

        /// <summary>
        /// Free-form detail describing why the search failed, when available (e.g. the match-prediction abandonment
        /// reason, or the number of batches that failed during processing). <c>null</c> when no further detail was
        /// supplied, consistent with the other optional fields on this type; <see cref="Summary"/> treats it as absent.
        /// </summary>
        public string FailureDetail { get; set; }

        /// <summary>
        /// Summary of the failure.
        /// </summary>
        public string Summary
        {
            get
            {
                var validationError = string.IsNullOrEmpty(MatchingAlgorithmFailureInfo?.ValidationError)
                    ? ""
                    : $", with validation error: {MatchingAlgorithmFailureInfo.ValidationError}";

                var detail = string.IsNullOrEmpty(FailureDetail)
                    ? ""
                    : $": {FailureDetail}";

                return $"Search failed at stage: {StageReached}{validationError}{detail}";
            }
        }
    }
}