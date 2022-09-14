using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;

namespace Atlas.MatchPrediction.ExternalInterface.Models
{
    public class IdentifiedMatchPredictionRequest
    {
        public SingleDonorMatchProbabilityInput SingleDonorMatchProbabilityInput { get; set; }
        public string Id { get; set; }
    }
}