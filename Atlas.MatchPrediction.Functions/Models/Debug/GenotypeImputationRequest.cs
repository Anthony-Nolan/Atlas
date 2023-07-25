using System.Collections.Generic;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;

namespace Atlas.MatchPrediction.Functions.Models.Debug
{
    public class GenotypeImputationRequest
    {
        public PhenotypeInfoTransfer<string> HlaTyping { get; set; }
        public IEnumerable<Locus> AllowedLoci { get; set; }
        public FrequencySetMetadata FrequencySetMetadata { get; set; }
    }
}