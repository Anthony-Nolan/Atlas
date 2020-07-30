using System.Collections.Generic;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Utils.Models;

namespace Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability
{
    public class MatchProbabilityResponse
    {
        public Probability ZeroMismatchProbability { get; set; }
        public Probability OneMismatchProbability { get; set; }
        public Probability TwoMismatchProbability { get; set; }
        public LociInfo<Probability> ZeroMismatchProbabilityPerLocus { get; set; }
        public LociInfo<Probability> OneMismatchProbabilityPerLocus { get; set; }
        public LociInfo<Probability> TwoMismatchProbabilityPerLocus { get; set; }
        public bool IsPatientPhenotypeUnrepresented { get; set; }
        public bool IsDonorPhenotypeUnrepresented { get; set; }

        public MatchProbabilityResponse()
        {
        }

        /// <summary>
        /// Used to initialise a response when all probabilities are known upfront.
        /// This can be useful for e.g. Shortcuts when a mismatch is guaranteed.
        /// </summary>
        public MatchProbabilityResponse(Probability sharedProbability, ISet<Locus> allowedLoci)
        {
            ZeroMismatchProbability = sharedProbability;
            OneMismatchProbability = sharedProbability;
            TwoMismatchProbability = sharedProbability;
            ZeroMismatchProbabilityPerLocus = new LociInfo<Probability>(sharedProbability).Map((l, v) =>
                allowedLoci.Contains(l) ? v : null
            );
            OneMismatchProbabilityPerLocus = new LociInfo<Probability>(sharedProbability).Map((l, v) =>
                allowedLoci.Contains(l) ? v : null
            );
            TwoMismatchProbabilityPerLocus = new LociInfo<Probability>(sharedProbability).Map((l, v) =>
                allowedLoci.Contains(l) ? v : null
            );
        }

        public MatchProbabilityResponse Round(int decimalPlaces)
        {
            return new MatchProbabilityResponse
            {
                ZeroMismatchProbability = ZeroMismatchProbability?.Round(decimalPlaces),
                OneMismatchProbability = OneMismatchProbability?.Round(decimalPlaces),
                TwoMismatchProbability = TwoMismatchProbability?.Round(decimalPlaces), 
                ZeroMismatchProbabilityPerLocus = ZeroMismatchProbabilityPerLocus.Map(p => p?.Round(decimalPlaces)),
                OneMismatchProbabilityPerLocus = OneMismatchProbabilityPerLocus.Map(p => p?.Round(decimalPlaces)),
                TwoMismatchProbabilityPerLocus = TwoMismatchProbabilityPerLocus.Map(p => p?.Round(decimalPlaces))
            };
        }
    }
}