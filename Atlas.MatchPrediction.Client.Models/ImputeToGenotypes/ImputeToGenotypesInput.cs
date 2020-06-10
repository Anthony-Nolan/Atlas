using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.Client.Models.ImputeToGenotypes
{
    public class ImputeToGenotypesInput
    {
        public PhenotypeInfo<string> Phenotype { get; set; }
        public string HlaNomenclatureVersion { get; set; }
    }
}