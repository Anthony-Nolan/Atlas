using Atlas.MatchingAlgorithm.Client.Models.SearchResults;

namespace Atlas.Functions.Models.Search.Results
{
    public class SearchResult
    {
        public MatchingAlgorithmResult MatchingResult { get; set; }
        public decimal MatchPredictionResult { get; set; }
    }
}