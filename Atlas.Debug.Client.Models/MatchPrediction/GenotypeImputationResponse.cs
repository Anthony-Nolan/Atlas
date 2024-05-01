using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.Public.Models.MatchPrediction;

namespace Atlas.Debug.Client.Models.MatchPrediction
{
    public class GenotypeImputationResponse
    {
        public string HlaTyping { get; set; }
        public MatchPredictionParameters MatchPredictionParameters { get; set; }
        public HaplotypeFrequencySet HaplotypeFrequencySet { get; set; }
        public bool IsUnrepresented => GenotypeCount == 0;
        public int GenotypeCount { get; set; }
        public decimal SumOfLikelihoods { get; set; }
        public string GenotypeLikelihoods { get; set; }
    }
}