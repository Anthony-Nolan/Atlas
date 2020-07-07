using Atlas.MatchingAlgorithm.Client.Models.SearchResults;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;

namespace Atlas.Functions.Models.Search.Results
{
    public class SearchResult
    {
        public MatchingAlgorithmResult MatchingResult { get; set; }
        public MatchProbabilityResponse MatchPredictionResult { get; set; }
    }
}