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
        public decimal ZeroMismatchProbability { get; set; }
        public decimal OneMismatchProbability { get; set; }
        public decimal TwoMismatchProbability { get; set; }
        public LociInfo<decimal?> ZeroMismatchProbabilityPerLocus { get; set; }
    }
}