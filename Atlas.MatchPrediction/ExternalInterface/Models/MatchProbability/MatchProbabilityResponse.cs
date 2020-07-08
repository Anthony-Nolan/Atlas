using System.Linq;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Config;

namespace Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability
{
    public class MatchProbabilityResponse
    {
        public MatchProbabilityResponse()
        {
        }

        /// <summary>
        /// Used to initialise a response when all probabilities are known upfront.
        /// This can be useful for e.g. Shortcuts when a mismatch is guaranteed.
        /// </summary>
        public MatchProbabilityResponse(Probability sharedProbability)
        {
            ZeroMismatchProbability = sharedProbability;
            ZeroMismatchProbabilityPerLocus = new LociInfo<Probability>(sharedProbability)
                .Map((l, v) =>
                    LocusSettings.MatchPredictionLoci.ToList().Contains(l) ? v : null
                );
        }

        public Probability ZeroMismatchProbability { get; set; }
        public LociInfo<Probability> ZeroMismatchProbabilityPerLocus { get; set; }

        public MatchProbabilityResponse Round(int decimalPlaces)
        {
            return new MatchProbabilityResponse
            {
                ZeroMismatchProbability = ZeroMismatchProbability.Round(decimalPlaces),
                ZeroMismatchProbabilityPerLocus = ZeroMismatchProbabilityPerLocus.Map(p => p?.Round(decimalPlaces))
            };
        }
    }
}