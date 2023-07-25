using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.ExternalInterface.Models;
using System.Collections.Generic;

namespace Atlas.MatchPrediction.Models
{
    internal struct ImputedGenotypes
    {
        public Dictionary<PhenotypeInfo<string>, decimal> GenotypeLikelihoods { get; set; }
        public ISet<PhenotypeInfo<HlaAtKnownTypingCategory>> Genotypes { get; set; }
    }
}