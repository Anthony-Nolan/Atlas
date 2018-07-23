using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Client.Models.SearchResults
{
    public class SearchResultSet
    {
        public int TotalResults { get; set; }
        public IEnumerable<SearchResult> SearchResults { get; set; }
    }
}