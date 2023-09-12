using System.Collections.Generic;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;

namespace Atlas.MatchPrediction.Functions.Models.Debug
{
    public class GenotypeImputationResponse
    {
        public string HlaTyping { get; set; }
        public IEnumerable<Locus> AllowedLoci { get; set; }
        public HaplotypeFrequencySet HaplotypeFrequencySet { get; set; }
        public bool IsUnrepresented => GenotypeCount == 0;
        public int GenotypeCount { get; set; }
        public decimal SumOfLikelihoods { get; set; }
        public string GenotypeLikelihoods { get; set; }
    }
}