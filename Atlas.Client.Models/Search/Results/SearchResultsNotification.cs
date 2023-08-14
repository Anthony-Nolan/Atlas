using System;
using Atlas.Client.Models.Search.Results.LogFile;

// ReSharper disable MemberCanBeInternal
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Atlas.Client.Models.Search.Results
{
    public class SearchResultsNotification : ResultsNotification
    {
        /// <summary>
        /// If the search was not a success, this should be populated to indicate which stage of search failed. 
        /// </summary>
        [Obsolete($"Superseded by {nameof(FailureInfo)}.{nameof(SearchFailureInfo.Summary)}. {nameof(FailureMessage)} will be only be populated for backwards compatibility.")]
        public string FailureMessage => FailureInfo?.Summary;

        /// <summary>
        ///<inheritdoc cref="SearchFailureInfo"/>
        /// Only set when <see cref="ResultsNotification.WasSuccessful"/> is `false`.
        /// </summary>
        public SearchFailureInfo FailureInfo { get; set; }

        /// <summary>
        /// Time taken to run the matching algorithm search step
        ///     - Does not include results upload
        /// </summary>
        [Obsolete($"Superseded by {nameof(RequestPerformanceMetrics.AlgorithmCoreStepDuration)} in matching-algorithm-results log file")]
        public TimeSpan MatchingAlgorithmTime { get; set; }

        /// <summary>
        /// Total time taken to run the match prediction algorithm for all results.
        ///
        /// Note that this can run in parallel - the logged time is the time between starting running MPA requests, and getting the last results.
        /// The sum of all MPA processing time may exceed this, if donors were calculated in parallel.
        /// </summary>
        [Obsolete($"Superseded by {nameof(RequestPerformanceMetrics.AlgorithmCoreStepDuration)} in atlas-search-results log file")]
        public TimeSpan MatchPredictionTime { get; set; }

        /// <summary>
        /// Time taken to run the matching algorithm search step added to search orchestration time
        /// 
        /// Will exceed the sum of matching algorithm search step and match prediction, as this time also includes:
        ///     - Fetching donor metadata to use in the match prediction algorithm
        ///     - Conversion of search results
        ///     - Persisting results to Azure storage
        ///     - Any other plumbing / orchestration time.
        ///
        /// Does not track the queue time
        /// </summary>
        [Obsolete($"Superseded by {nameof(RequestPerformanceMetrics.Duration)}")]
        public TimeSpan OverallSearchTime { get; set; }
    }
}