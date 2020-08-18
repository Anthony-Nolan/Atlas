using System;
using System.Collections.Generic;
// ReSharper disable MemberCanBeInternal
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Atlas.Client.Models.Search.Results
{
    public class SearchResultSet 
    {
        public string SearchRequestId { get; set; }
        public int TotalResults { get; set; }
        public IEnumerable<SearchResult> SearchResults { get; set; }

        public string HlaNomenclatureVersion { get; set; }
        public string BlobStorageContainerName { get; set; }
        public string ResultsFileName => $"{SearchRequestId}.json";
        
        public TimeSpan MatchingAlgorithmTime { get; set; }
        public TimeSpan MatchPredictionTime { get; set; }
    }
}