using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Public.Models.MatchPrediction;

namespace Atlas.Debug.Client.Models.MatchPrediction
{
    public class SubjectInfo
    {
        public PhenotypeInfoTransfer<string> HlaTyping { get; set; }
        public FrequencySetMetadata FrequencySetMetadata { get; set; }
    }
}