using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.MatchPrediction;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Atlas.Client.Models.Search.Results
{
    public class SearchResult : IResult
    {
        public string DonorCode { get; set; }
        public MatchingAlgorithmResult MatchingResult { get; set; }
        public MatchProbabilityResponse MatchPredictionResult { get; set; }

        public ScoringResult ScoringResult => MatchingResult.ScoringResult;
    }
}