using System;

// ReSharper disable MemberCanBeInternal
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Atlas.Client.Models.Search.Results
{
    public class SearchResultsNotification : ResultsNotification
    {
        /// <summary>
        /// If the search was not a success, this should be populated to indicate which stage of search failed. 
        /// </summary>
        [Obsolete(message: $"Superseded by {nameof(FailureInfo)}. {nameof(FailureMessage)} will be only be populated for backwards compatibility.")]
        public string FailureMessage { get; set; }

        /// <summary>
        /// Information related to search request failure; only set when <see cref="ResultsNotification.WasSuccessful"/> is `false`.
        /// </summary>
        public SearchFailureInfo FailureInfo { get; set; }

        /// <summary>
        /// Time taken to run the matching algorithm - currently includes matching, and scoring.
        /// </summary>
        public TimeSpan MatchingAlgorithmTime { get; set; }

        /// <summary>
        /// Total time taken to run the match prediction algorithm for all results.
        ///
        /// Note that this can run in parallel - the logged time is the time between starting running MPA requests, and getting the last results.
        /// The sum of all MPA processing time may exceed this, if donors were calculated in parallel.
        /// </summary>
        public TimeSpan MatchPredictionTime { get; set; }

        /// <summary>
        /// Total time between search initiation and results notification.
        /// 
        /// Will exceed the sum of matching algorithm and match prediction, as this time also includes:
        ///     - Fetching donor metadata to use in the match prediction algorithm
        ///     - Conversion of search results
        ///     - Persisting results to Azure storage
        ///     - Any other plumbing / orchestration time.
        /// </summary>
        public TimeSpan OverallSearchTime { get; set; }
    }
}