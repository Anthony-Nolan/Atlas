using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;

namespace Atlas.MatchPrediction.Functions.Models.Debug
{
    public class SubjectInfo
    {
        public PhenotypeInfoTransfer<string> HlaTyping { get; set; }
        public FrequencySetMetadata FrequencySetMetadata { get; set; }
    }
}