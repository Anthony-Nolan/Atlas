using Atlas.Common.Utils.Models;
using Newtonsoft.Json;

namespace Atlas.Client.Models.Search.Results.MatchPrediction
{
    public class MatchProbabilities
    {
        public Probability ZeroMismatchProbability { get; set; }
        public Probability OneMismatchProbability { get; set; }
        public Probability TwoMismatchProbability { get; set; }

        [JsonIgnore]
        public PredictiveMatchCategory? MatchCategory => ZeroMismatchProbability?.Percentage switch
        {
            100 => PredictiveMatchCategory.Exact,
            0 => PredictiveMatchCategory.Mismatch,
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
