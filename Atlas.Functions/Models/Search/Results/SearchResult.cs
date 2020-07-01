namespace Atlas.Functions.Models.Search.Results
{
    public class SearchResult
    {
        public MatchingAlgorithm.Client.Models.SearchResults.MatchingAlgorithmResult MatchingResult { get; set; }
        public decimal MatchPredictionResult { get; set; }
    }
}