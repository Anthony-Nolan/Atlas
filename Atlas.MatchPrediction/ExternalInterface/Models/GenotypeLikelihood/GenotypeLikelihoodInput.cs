using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.ExternalInterface.Models.GenotypeLikelihood
{
    public class GenotypeLikelihoodInput
    {
        public PhenotypeInfo<string> Genotype { get; set; }
        public FrequencySetMetadata PatientFrequencySetMetadata { get; set; }
        public FrequencySetMetadata DonorFrequencySetMetadata { get; set; }
        public bool IsPatient { get; set; }
    }
}