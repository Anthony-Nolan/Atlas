using System.Collections.Generic;
using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.ExternalInterface.Models.ExpandAmbiguousPhenotype
{
    public class ExpandAmbiguousPhenotypeResponse
    {
        public IEnumerable<PhenotypeInfo<string>> Genotypes { get; set; }
    }
}
