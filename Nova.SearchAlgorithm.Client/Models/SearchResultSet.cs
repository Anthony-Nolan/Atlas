using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Client.Models
{
    public class SearchResultSet
    {
        public int TotalResults { get; set; }
        public IEnumerable<PotentialMatch> SearchResults { get; set; }
    }
}