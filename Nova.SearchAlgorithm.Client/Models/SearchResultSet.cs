using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Client.Models
{
    public class SearchResultSet
    {
        public int SearchRequestId { get; set; }
        public List<SearchResult> SearchResults { get; set; }
    }
}