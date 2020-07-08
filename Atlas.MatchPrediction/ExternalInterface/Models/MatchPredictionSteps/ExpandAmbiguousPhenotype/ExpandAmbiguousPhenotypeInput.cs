using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.ExternalInterface.Models.MatchPredictionSteps.ExpandAmbiguousPhenotype
{
    public class ExpandAmbiguousPhenotypeInput
    {
        public PhenotypeInfo<string> Phenotype { get; set; }
        public string HlaNomenclatureVersion { get; set; }
    }
}