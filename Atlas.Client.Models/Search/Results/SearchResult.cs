using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.MatchPrediction;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Atlas.Client.Models.Search.Results
{
    public class SearchResult
    {
        // Referred to as "external donor code" throughout the codebase. Here we no longer refer to it as "external", as this model is consumer-facing. 
        public string DonorCode { get; set; }
        public MatchingAlgorithmResult MatchingResult { get; set; }
        public MatchProbabilityResponse MatchPredictionResult { get; set; }
    }
}