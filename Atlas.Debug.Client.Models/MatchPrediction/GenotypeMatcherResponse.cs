﻿using System.Collections.Generic;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.Public.Models.MatchPrediction;

namespace Atlas.Debug.Client.Models.MatchPrediction
{
    public class GenotypeMatcherResponse
    {
        public MatchPredictionParameters MatchPredictionParameters { get; set; }
        public SubjectResult PatientInfo { get; set; }
        public SubjectResult DonorInfo { get; set; }

        /// <summary>
        /// Patient-donor genotype pairs and their match counts.
        /// </summary>
        public IEnumerable<string> MatchedGenotypePairs { get; set; }
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