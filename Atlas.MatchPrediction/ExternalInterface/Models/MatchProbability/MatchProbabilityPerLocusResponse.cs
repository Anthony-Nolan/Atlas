using System;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Utils.Models;

namespace Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability
{
    public class MatchProbabilityPerLocusResponse
    {
        public MatchProbabilities MatchProbabilities { get; set; }

        public LocusInfo<PredictiveMatchCategory> PositionalMatchCategories => MatchProbabilities.MatchCategory switch
        {
            PredictiveMatchCategory.Exact => new LocusInfo<PredictiveMatchCategory>(PredictiveMatchCategory.Exact),
            PredictiveMatchCategory.Mismatch => new LocusInfo<PredictiveMatchCategory>(PredictiveMatchCategory.Mismatch,
                MatchProbabilities.TwoMismatchProbability.Decimal == 0 ? PredictiveMatchCategory.Exact :
                MatchProbabilities.OneMismatchProbability.Decimal == 0 ? PredictiveMatchCategory.Mismatch : PredictiveMatchCategory.Potential),
            PredictiveMatchCategory.Potential => new LocusInfo<PredictiveMatchCategory>(PredictiveMatchCategory.Potential,
                MatchProbabilities.TwoMismatchProbability.Decimal == 0 ? PredictiveMatchCategory.Potential : PredictiveMatchCategory.Exact),
            _ => throw new ArgumentOutOfRangeException()
        };

        public MatchProbabilityPerLocusResponse()
        {
        }

        public MatchProbabilityPerLocusResponse(Probability sharedProbability)
        {
            MatchProbabilities = new MatchProbabilities(sharedProbability);
        }

        public MatchProbabilityPerLocusResponse(MatchProbabilities sharedMatchProbabilities)
        {
            MatchProbabilities = sharedMatchProbabilities;
        }

        public MatchProbabilityPerLocusResponse Round(int decimalPlaces)
        {
            return new MatchProbabilityPerLocusResponse
            {
                MatchProbabilities = MatchProbabilities?.Round(decimalPlaces)
            };
        }
    }
}
