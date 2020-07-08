using Atlas.MatchingAlgorithm.Client.Models.SearchResults;

namespace Atlas.Functions.Models.Search.Results
{
    public class SearchResult
    {
        // Referred to as "external donor code" throughout the codebase. Here we refer to it as an ID, as this model is consumer-facing. 
        public string DonorId { get; set; }
        public MatchingAlgorithmResult MatchingResult { get; set; }
        public decimal MatchPredictionResult { get; set; }
    }
}