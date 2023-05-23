using System;

// ReSharper disable MemberCanBeInternal
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Atlas.Client.Models.Search.Results.ResultSet
{
    public abstract class SearchResultSet : ResultSet<SearchResult>
    {
        /// <summary>
        /// Time taken to run the matching algorithm search step
        ///     - Does not include results upload
        /// </summary>
        public TimeSpan MatchingAlgorithmTime { get; set; }

        /// <summary>
        /// Total time taken to process all match prediction requests for all results - excludes any orchestration time that takes place before or after this ste
        /// </summary>
        public TimeSpan MatchPredictionTime { get; set; }
    }
}