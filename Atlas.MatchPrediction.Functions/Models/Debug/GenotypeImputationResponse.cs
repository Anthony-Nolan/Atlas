using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Models;

namespace Atlas.MatchPrediction.Functions.Models.Debug
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