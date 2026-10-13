using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Public.Models.MatchPrediction;

namespace Atlas.MatchPrediction.Functions.Models.Debug
{
    internal class SubjectInfo
    {
        public PhenotypeInfoTransfer<string> HlaTyping { get; set; }
        public FrequencySetMetadata FrequencySetMetadata { get; set; }
    }
}