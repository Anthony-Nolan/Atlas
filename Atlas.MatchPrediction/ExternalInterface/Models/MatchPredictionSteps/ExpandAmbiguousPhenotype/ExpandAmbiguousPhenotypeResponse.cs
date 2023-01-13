using System.Collections.Generic;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Models;

namespace Atlas.MatchPrediction.ExternalInterface.Models.MatchPredictionSteps.ExpandAmbiguousPhenotype
{
    public class ExpandAmbiguousPhenotypeResponse
    {
        public ISet<PhenotypeInfo<HlaAtKnownTypingCategory>> Genotypes { get; set; }
    }
}
