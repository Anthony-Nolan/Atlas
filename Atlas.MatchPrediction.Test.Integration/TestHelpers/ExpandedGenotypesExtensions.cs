using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.Services.CompressedPhenotypeExpansion;

namespace Atlas.MatchPrediction.Test.Integration.TestHelpers;

internal static class ExpandedGenotypesExtensions
{
    /// <summary>
    /// Every genotype the expansion produced, built.
    ///
    /// <para>
    /// An expansion holds each genotype as the two pool indices it is a pair of, and builds the
    /// <see cref="PhenotypeInfo{T}"/> only for the genotypes truncation keeps - up to 1.65M pairs of which a capped
    /// donor keeps 2,000. Materialising all of them defeats that, so <b>production must never call this</b>; a test
    /// asserting on a handful of genotypes can.
    /// </para>
    /// </summary>
    internal static List<PhenotypeInfo<HlaAtKnownTypingCategory>> MaterialiseAll(this ExpandedGenotypes expanded) =>
        Enumerable.Range(0, expanded.GenotypeCount).Select(expanded.Materialise).ToList();
}
