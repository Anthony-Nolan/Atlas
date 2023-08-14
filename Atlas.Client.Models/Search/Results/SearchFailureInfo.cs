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
        /// </summary>
        public bool WillRetry => MatchingAlgorithmFailureInfo is { RemainingRetriesCount: > 0 };

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

                return $"Search failed at stage: {StageReached}{validationError}";
            }
        }
    }
}