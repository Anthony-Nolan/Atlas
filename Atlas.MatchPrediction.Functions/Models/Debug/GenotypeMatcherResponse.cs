using System.Collections.Generic;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;

namespace Atlas.MatchPrediction.Functions.Models.Debug
{
    public class GenotypeMatcherResponse
    {
        public IEnumerable<Locus> AllowedLoci { get; set; }
        public SubjectResult PatientInfo { get; set; }
        public SubjectResult DonorInfo { get; set; }

        /// <summary>
        /// Collection of <see cref="MatchedGenotypePair"/> (value) for every distinct match count (key) observed.
        /// </summary>
        public Dictionary<int, IEnumerable<MatchedGenotypePair>> MatchedGenotypePairs { get; set; }
    }

    public class SubjectResult
    {
        public bool IsUnrepresented { get; set; }
        public HaplotypeFrequencySet HaplotypeFrequencySet { get; set; }
        public string HlaTyping { get; set; }

        public SubjectResult(bool isUnrepresented, HaplotypeFrequencySet haplotypeFrequencySet, string hlaTyping)
        {
            IsUnrepresented = isUnrepresented;
            HaplotypeFrequencySet = haplotypeFrequencySet;
            HlaTyping = hlaTyping;
        }
    }

    public class MatchedGenotypePair
    {
        public string PatientGenotype { get; set; }
        public string DonorGenotype { get; set; }

        /// <summary>
        /// Total match count per <see cref="MatchedGenotypePair"/> followed by counts at each locus.
        /// </summary>
        public string MatchCounts { get; set; }
    }
}