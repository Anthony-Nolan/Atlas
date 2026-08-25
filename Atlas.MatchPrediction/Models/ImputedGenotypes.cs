using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.ExternalInterface.Models;
using System.Collections.Generic;

// A genotype's HLA names at whatever resolution the haplotype frequency set stored them - P group, or G group where a
// null allele meant no P group existed - with the typing category deliberately ERASED, which is why two genotypes
// differing only in category collapse to one key here.
using HfSetGenotypeNames = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.PhenotypeInfo<string>;

namespace Atlas.MatchPrediction.Models
{
    public struct ImputedGenotypes
    {
        public Dictionary<HfSetGenotypeNames, decimal> GenotypeLikelihoods { get; set; }
        public ISet<PhenotypeInfo<HlaAtKnownTypingCategory>> Genotypes { get; set; }
        public decimal SumOfLikelihoods { get; set; }

        public static ImputedGenotypes Empty()
        {
            return new ImputedGenotypes
            {
                GenotypeLikelihoods = new Dictionary<HfSetGenotypeNames, decimal>(),
                Genotypes = new HashSet<PhenotypeInfo<HlaAtKnownTypingCategory>>(),
                SumOfLikelihoods = 0
            };
        }
    }
}
