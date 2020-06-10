using System.Collections.Generic;
using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.Client.Models.ImputeToGenotypes
{
    public class ImputeToGenotypesResponse
    {
        public IEnumerable<PhenotypeInfo<string>> Genotypes { get; set; }
    }
}
