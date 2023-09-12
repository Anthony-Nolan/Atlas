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
        /// Patient-donor genotype pairs (represented as a single, formatted string) and their match counts.
        /// </summary>
        public string MatchedGenotypePairs { get; set; }
    }

    public class SubjectResult
    {
        public bool IsUnrepresented { get; set; }
        public decimal SumOfLikelihoods { get; set; }
        public HaplotypeFrequencySet HaplotypeFrequencySet { get; set; }
        public string HlaTyping { get; set; }

        public SubjectResult(
            bool isUnrepresented,
            decimal sumOfLikelihoods,
            HaplotypeFrequencySet haplotypeFrequencySet,
            string hlaTyping)
        {
            IsUnrepresented = isUnrepresented;
            SumOfLikelihoods = sumOfLikelihoods;
            HaplotypeFrequencySet = haplotypeFrequencySet;
            HlaTyping = hlaTyping;
        }
    }
}