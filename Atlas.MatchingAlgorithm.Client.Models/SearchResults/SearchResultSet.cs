using System.Collections.Generic;

namespace Atlas.MatchingAlgorithm.Client.Models.SearchResults
{
    public class SearchResultSet
    {
        public string SearchRequestId { get; set; }
        public int TotalResults { get; set; }
        public IEnumerable<SearchResult> SearchResults { get; set; }

        public string HlaNomenclatureVersion { get; set; }
        public string BlobStorageContainerName { get; set; }
        public string ResultsFileName => $"{SearchRequestId}.json";
    }
}