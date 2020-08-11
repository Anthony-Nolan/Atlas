using Atlas.Common.Utils.Models;
using Newtonsoft.Json;

namespace Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability
{
    public class MatchProbabilities
    {
        public Probability ZeroMismatchProbability { get; set; }
        public Probability OneMismatchProbability { get; set; }
        public Probability TwoMismatchProbability { get; set; }

        [JsonIgnore]
        public PredictiveMatchCategory? MatchCategory => ZeroMismatchProbability?.Decimal switch
        {
            1m => PredictiveMatchCategory.Exact,
            0m => PredictiveMatchCategory.Mismatch,
            null => null,
            _ => PredictiveMatchCategory.Potential
        };

        public MatchProbabilities()
        {
        }

        public MatchProbabilities(Probability sharedProbability)
        {
            ZeroMismatchProbability = sharedProbability;
            OneMismatchProbability = sharedProbability;
            TwoMismatchProbability = sharedProbability;
        }

        public MatchProbabilities Round(int decimalPlaces)
        {
            return new MatchProbabilities
            {
                ZeroMismatchProbability = ZeroMismatchProbability?.Round(decimalPlaces),
                OneMismatchProbability = OneMismatchProbability?.Round(decimalPlaces),
                TwoMismatchProbability = TwoMismatchProbability?.Round(decimalPlaces)
            };
        }
    }
}
