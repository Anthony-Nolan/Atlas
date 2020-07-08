using Atlas.MatchingAlgorithm.Client.Models.SearchResults;

namespace Atlas.Functions.Models.Search.Results
{
    public class SearchResult
    {
        // Referred to as "external donor code" throughout the codebase. Here we no longer refer to it as "external", as this model is consumer-facing. 
        public string DonorCode { get; set; }
        public MatchingAlgorithmResult MatchingResult { get; set; }
        public decimal MatchPredictionResult { get; set; }
    }
}