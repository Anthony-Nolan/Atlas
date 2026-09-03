using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.Models;

namespace Atlas.MatchPrediction.Test.TestHelpers;

/// <summary>
/// Two views of <see cref="ImputedGenotypes"/> that the result no longer carries itself: a set of genotypes, and a
/// likelihood dictionary keyed by name form. Kept here so that assertions can be written against the values a caller
/// cares about, rather than against the shape the result is carried in.
/// </summary>
internal static class ImputedGenotypesExtensions
{
    /// <summary>The kept genotypes alone, in the order the expansion produced them.</summary>
    public static IReadOnlyList<PhenotypeInfo<HlaAtKnownTypingCategory>> GenotypesOnly(this ImputedGenotypes imputed) =>
        imputed.Genotypes.Select(genotype => genotype.Genotype).ToList();

    /// <summary>
    /// One entry per surviving name form. Grouped rather than <c>ToDictionary</c> because two genotypes can share a
    /// name form - the collapse case - and they carry one likelihood between them.
    /// </summary>
    public static Dictionary<PhenotypeInfo<string>, decimal> LikelihoodsByName(this ImputedGenotypes imputed) =>
        imputed.Genotypes
            .GroupBy(genotype => genotype.Names)
            .ToDictionary(group => group.Key, group => group.First().Likelihood);
}
