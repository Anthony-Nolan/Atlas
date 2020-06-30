using System.Collections.Generic;

namespace Atlas.MatchingAlgorithm.Client.Models.SearchResults
{
    public class MatchingAlgorithmResultSet
    {
        public string SearchRequestId { get; set; }
        public int TotalResults { get; set; }
        public IEnumerable<MatchingAlgorithmResult> SearchResults { get; set; }

        public string HlaNomenclatureVersion { get; set; }
        public string BlobStorageContainerName { get; set; }
        public string ResultsFileName => $"{SearchRequestId}.json";
    }
}