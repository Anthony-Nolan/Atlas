using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Models;

namespace Atlas.MatchPrediction.Functions.Models.Debug
{
    public class GenotypeMatcherResponse
    {
        public MatchPredictionParameters MatchPredictionParameters { get; set; }
        public SubjectResult PatientInfo { get; set; }
        public SubjectResult DonorInfo { get; set; }

        /// <summary>
        /// Patient-donor genotype pairs (represented as a single, formatted string) and their match counts.
        /// </summary>
        public string MatchedGenotypePairs { get; set; }
    }

    public class SubjectResult
    {
        public bool IsUnrepresented { get; set; }
        public int GenotypeCount { get; set; }
        public decimal SumOfLikelihoods { get; set; }
        public HaplotypeFrequencySet HaplotypeFrequencySet { get; set; }
        public string HlaTyping { get; set; }

        public SubjectResult(
            bool isUnrepresented,
            int genotypeCount,
            decimal sumOfLikelihoods,
            HaplotypeFrequencySet haplotypeFrequencySet,
            string hlaTyping)
        {
            IsUnrepresented = isUnrepresented;
            GenotypeCount = genotypeCount;
            SumOfLikelihoods = sumOfLikelihoods;
            HaplotypeFrequencySet = haplotypeFrequencySet;
            HlaTyping = hlaTyping;
        }
    }
}