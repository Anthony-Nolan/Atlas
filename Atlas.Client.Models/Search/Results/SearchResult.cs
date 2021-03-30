using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.MatchPrediction;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Atlas.Client.Models.Search.Results
{
    public class SearchResult : Result
    {
        public MatchingAlgorithmResult MatchingResult { get; set; }
        public MatchProbabilityResponse MatchPredictionResult { get; set; }

        public override ScoringResult ScoringResult => MatchingResult.ScoringResult;
    }
}