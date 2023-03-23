using System;

// ReSharper disable MemberCanBeInternal
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Atlas.Client.Models.Search.Results.ResultSet
{
    public abstract class SearchResultSet : ResultSet<SearchResult>
    {
        public TimeSpan MatchingAlgorithmTime { get; set; }
        public TimeSpan MatchPredictionTime { get; set; }
    }
}