using System.Collections.Generic;
using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.Client.Models.GenotypeImputation
{
    public class GenotypeImputationResponse
    {
        public List<PhenotypeInfo<string>> Genotypes { get; set; }
    }
}
