using Atlas.Client.Models.Search.Results.LogFile;
using System;

namespace Atlas.Client.Models.Search.Results.ResultSet
{
    public abstract class SearchResultSet : ResultSet<SearchResult>
    {
        [Obsolete($"Superseded by {nameof(RequestPerformanceMetrics.AlgorithmCoreStepDuration)} in matching-algorithm-results log file")]
        public TimeSpan MatchingAlgorithmTime { get; set; }

        [Obsolete($"Superseded by {nameof(RequestPerformanceMetrics.AlgorithmCoreStepDuration)} in atlas-search-results log file")]
        public TimeSpan MatchPredictionTime { get; set; }
    }
}