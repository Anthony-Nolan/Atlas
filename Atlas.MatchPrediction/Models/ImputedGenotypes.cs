using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.ExternalInterface.Models;
using System.Collections.Generic;

namespace Atlas.MatchPrediction.Models
{
    public struct ImputedGenotypes
    {
        public Dictionary<PhenotypeInfo<string>, decimal> GenotypeLikelihoods { get; set; }
        public ISet<PhenotypeInfo<HlaAtKnownTypingCategory>> Genotypes { get; set; }
        public decimal SumOfLikelihoods { get; set; }

        public static ImputedGenotypes Empty()
        {
            return new ImputedGenotypes
            {
                GenotypeLikelihoods = new Dictionary<PhenotypeInfo<string>, decimal>(),
                Genotypes = new HashSet<PhenotypeInfo<HlaAtKnownTypingCategory>>(),
                SumOfLikelihoods = 0
            };
        }
    }
}