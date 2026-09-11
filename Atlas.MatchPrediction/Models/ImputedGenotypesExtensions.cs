using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.Models;

/// <summary>A view of <see cref="ImputedGenotypes"/> that the result no longer carries itself.</summary>
public static class ImputedGenotypesExtensions
{
    /// <summary>
    /// One entry per surviving name form. Grouped rather than <c>ToDictionary</c> because two genotypes can share a
    /// name form - the collapse case - and they carry one likelihood between them.
    /// </summary>
    public static Dictionary<PhenotypeInfo<string>, decimal> LikelihoodsByName(this ImputedGenotypes imputed) =>
        imputed.Genotypes
            .GroupBy(genotype => genotype.Names)
            .ToDictionary(group => group.Key, group => group.First().Likelihood);
}
