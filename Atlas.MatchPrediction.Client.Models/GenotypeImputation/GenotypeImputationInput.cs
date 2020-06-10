using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.Client.Models.GenotypeImputation
{
    public class GenotypeImputationInput
    {
        public PhenotypeInfo<string> Phenotype { get; set; }
        public string NomenclatureVersion { get; set; }
    }
}