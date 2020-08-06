using System.Collections.Generic;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Utils.Models;
using Newtonsoft.Json;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeInternal

namespace Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability
{
    public enum MatchType
    {
        Exact,
        Potential,
        Mismatch
    }

    public class MatchProbabilities
    {
        public Probability ZeroMismatchProbability { get; set; }
        public Probability OneMismatchProbability { get; set; }
        public Probability TwoMismatchProbability { get; set; }
        public MatchType MatchType => ZeroMismatchProbability.Decimal switch
        {
            1m => MatchType.Exact,
            0m => MatchType.Mismatch,
            _ => MatchType.Potential
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

    public class MatchProbabilityResponse
    {
        public MatchProbabilities MatchProbabilities { get; set; }
        public LociInfo<MatchProbabilities> MatchProbabilitiesPerLocus { get; set; }

        [JsonIgnore]
        public LociInfo<Probability> ZeroMismatchProbabilityPerLocus => MatchProbabilitiesPerLocus.Map(x => x?.ZeroMismatchProbability);

        [JsonIgnore]
        public LociInfo<Probability> OneMismatchProbabilityPerLocus => MatchProbabilitiesPerLocus.Map(x => x?.OneMismatchProbability);

        [JsonIgnore]
        public LociInfo<Probability> TwoMismatchProbabilityPerLocus => MatchProbabilitiesPerLocus.Map(x => x?.TwoMismatchProbability);

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
                allowedLoci.Contains(l) ? new MatchProbabilities(sharedProbability) : null
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