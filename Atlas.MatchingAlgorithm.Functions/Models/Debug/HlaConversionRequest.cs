using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;

namespace Atlas.MatchingAlgorithm.Functions.Models.Debug
{
    public class HlaConversionRequest
    {
        public Locus Locus { get; set; }
        public string HlaName { get; set; }
        public TargetHlaCategory TargetHlaCategory { get; set; }
    }
}
