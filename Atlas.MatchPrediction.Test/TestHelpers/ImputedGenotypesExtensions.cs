using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.Models;

namespace Atlas.MatchPrediction.Test.TestHelpers;

/// <summary>
/// A view of <see cref="ImputedGenotypes"/> that the result no longer carries itself: a set of genotypes, without
/// their name forms or likelihoods. Kept here so that assertions can be written against the values a caller cares
/// about, rather than against the shape the result is carried in. The name-keyed likelihood view,
/// <c>LikelihoodsByName</c>, lives in production code (<see cref="ImputedGenotypesExtensions"/> in
/// <c>Atlas.MatchPrediction.Models</c>) since <c>GenotypeImputationFunctions</c> needs the same projection.
/// </summary>
internal static class ImputedGenotypesTestExtensions
{
    /// <summary>The kept genotypes alone, in the order the expansion produced them.</summary>
    public static IReadOnlyList<PhenotypeInfo<HlaAtKnownTypingCategory>> GenotypesOnly(this ImputedGenotypes imputed) =>
        imputed.Genotypes.Select(genotype => genotype.Genotype).ToList();
}
