using System.Collections.Generic;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;

namespace Atlas.MatchPrediction.Functions.Models.Debug
{
    public class GenotypeImputationResponse
    {
        public HaplotypeFrequencySet HaplotypeFrequencySet { get; set; }
        public IEnumerable<string> GenotypeLikelihoods { get; set; }
    }
}