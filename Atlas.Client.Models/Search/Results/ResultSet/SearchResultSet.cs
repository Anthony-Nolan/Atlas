using Atlas.Client.Models.Search.Results.LogFile;
using System;

namespace Atlas.Client.Models.Search.Results.ResultSet
{
    public abstract class SearchResultSet : ResultSet<SearchResult>
    {
        /// <summary>
        /// Time taken to run the matching algorithm search step
        ///     - Does not include results upload
        /// </summary>
        [Obsolete($"Superseded by {nameof(RequestPerformanceMetrics.AlgorithmCoreStepDuration)} in matching-algorithm-results log file")]
        public TimeSpan MatchingAlgorithmTime { get; set; }

        /// <summary>
        /// Total time taken to process all match prediction requests for all results - excludes any orchestration time that takes place before or after this step
        /// </summary>
        [Obsolete($"Superseded by {nameof(RequestPerformanceMetrics.AlgorithmCoreStepDuration)} in atlas-search-results log file")]
        public TimeSpan MatchPredictionTime { get; set; }
    }
}