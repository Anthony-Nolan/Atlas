using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.ExternalInterface.Models;

namespace Atlas.MatchPrediction.Models
{
    
    public class PatientMatchPredictionInfo : PhenotypeInfoWithFrequencyMetadata { }
    
    public class DonorMatchPredictionInfo : PhenotypeInfoWithFrequencyMetadata { }
    public class PhenotypeInfoWithFrequencyMetadata
    {
        public FrequencySetMetadata FrequencyMetadata { get; set; }
        public PhenotypeInfo<string> PhenotypeInfo { get; set; }
    }
}