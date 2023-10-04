using Atlas.Common.Public.Models.GeneticData;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Atlas.MatchPrediction.Models
{
    /// <summary>
    /// Parameters controlling Match Prediction behaviour (in addition to <see cref="SubjectData"/>)
    /// </summary>
    public class MatchPredictionParameters
    {
        /// <summary>
        /// Loci at which match probabilities should be calculated.
        /// </summary>
        public ISet<Locus> AllowedLoci { get; set; }

        /// <summary>
        /// Optional: HLA version of the matching algorithm.
        /// Used when an individual's HLA typing cannot be explained using the HLA version of the referenced HF set.
        /// In this case, if <see cref="MatchingAlgorithmHlaNomenclatureVersion"/> is `null` (or is identical to the HLA version of the referenced HF set),
        /// then a second HLA lookup will not be attempted, resulting in the individual being deemed "unrepresented".
        /// </summary>
        public string MatchingAlgorithmHlaNomenclatureVersion { get; set; }

        [JsonConstructor]
        public MatchPredictionParameters()
        {
        }

        public MatchPredictionParameters(ISet<Locus> allowedLoci, string matchingAlgorithmHlaNomenclatureVersion = null)
        {
            AllowedLoci = allowedLoci;
            MatchingAlgorithmHlaNomenclatureVersion = matchingAlgorithmHlaNomenclatureVersion;
        }
    }
}
