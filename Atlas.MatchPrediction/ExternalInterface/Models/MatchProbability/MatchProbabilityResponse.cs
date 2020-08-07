using System.Collections.Generic;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Utils.Models;
using Newtonsoft.Json;

namespace Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability
{
    public class MatchProbabilityResponse
    {
        public MatchProbabilities MatchProbabilities { get; set; }
        public LociInfo<MatchProbabilityPerLocusResponse> MatchProbabilitiesPerLocus { get; set; }

        public int OverallMatchCount => ExactMatchCount * 2 + PotentialMatchCount;
        public int ExactMatchCount => MatchProbabilitiesPerLocus.Reduce((locus, value, accumulator) =>
            value.MatchProbabilities.MatchCategory == PredictiveMatchCategory.Exact ? accumulator + 1 : accumulator, 0);
        public int PotentialMatchCount => MatchProbabilitiesPerLocus.Reduce((locus, value, accumulator) =>  
            value.MatchProbabilities.MatchCategory == PredictiveMatchCategory.Potential ? accumulator + 1 : accumulator , 0);

        [JsonIgnore]
        public LociInfo<Probability> ZeroMismatchProbabilityPerLocus =>
            MatchProbabilitiesPerLocus.Map(x => x?.MatchProbabilities.ZeroMismatchProbability);

        [JsonIgnore]
        public LociInfo<Probability> OneMismatchProbabilityPerLocus =>
            MatchProbabilitiesPerLocus.Map(x => x?.MatchProbabilities.OneMismatchProbability);

        [JsonIgnore]
        public LociInfo<Probability> TwoMismatchProbabilityPerLocus =>
            MatchProbabilitiesPerLocus.Map(x => x?.MatchProbabilities.TwoMismatchProbability);

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
            MatchProbabilities = new MatchProbabilities(sharedProbability);
            MatchProbabilitiesPerLocus = new LociInfo<Probability>(sharedProbability).Map((l, v) =>
                allowedLoci.Contains(l) ? new MatchProbabilityPerLocusResponse(sharedProbability) : null
            );
        }

        public MatchProbabilityResponse Round(int decimalPlaces)
        {
            return new MatchProbabilityResponse
            {
                MatchProbabilities = MatchProbabilities.Round(decimalPlaces),
                MatchProbabilitiesPerLocus = MatchProbabilitiesPerLocus.Map((_, p) => p?.Round(decimalPlaces)),
                IsPatientPhenotypeUnrepresented = IsPatientPhenotypeUnrepresented,
                IsDonorPhenotypeUnrepresented = IsDonorPhenotypeUnrepresented
            };
        }
    }
}