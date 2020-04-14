using System.Collections.Generic;

namespace Atlas.MatchingAlgorithm.Client.Models.SearchResults
{
    public class SearchResultSet
    {
        public int TotalResults { get; set; }
        public IEnumerable<SearchResult> SearchResults { get; set; }
    }
}