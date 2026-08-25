using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.ExternalInterface.Models;
using System.Collections.Generic;

// A genotype's HLA names at whatever resolution the haplotype frequency set stored them - P group, or G group where a
// null allele meant no P group existed - with the typing category deliberately ERASED, which is why two genotypes
// differing only in category share one of these.
using HfSetGenotypeNames = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.PhenotypeInfo<string>;

namespace Atlas.MatchPrediction.Models
{
    /// <summary>
    /// One genotype that truncation kept, with the two other things every consumer needs about it: the HLA-name form of
    /// it, and its likelihood.
    /// </summary>
    /// <remarks>
    /// The three are carried together rather than as a set of genotypes plus a name-keyed likelihood dictionary, so
    /// that no consumer has to rebuild a genotype's name form in order to look its likelihood up. The truncater has
    /// both in hand at the moment it selects the genotype.
    ///
    /// <para>
    /// <see cref="Names"/> is shared, not per genotype: it is built once per surviving <c>GenotypeNameKey</c>, and two
    /// genotypes that differ only in typing category hold the same reference.
    /// </para>
    /// </remarks>
    public readonly record struct ImputedGenotype(
        PhenotypeInfo<HlaAtKnownTypingCategory> Genotype,
        HfSetGenotypeNames Names,
        decimal Likelihood);

    public struct ImputedGenotypes
    {
        /// <summary>
        /// The kept genotypes, in the order the expansion produced them - which is pairing order, hence survivor order,
        /// hence the projected pool's order. Downstream sums run over this sequence, so it is part of the result.
        /// </summary>
        public IReadOnlyList<ImputedGenotype> Genotypes { get; set; }

        /// <summary>
        /// Summed over the surviving <b>name forms</b>, most likely first - not over <see cref="Genotypes"/>, which can
        /// hold two entries sharing one name form and one likelihood.
        /// </summary>
        public decimal SumOfLikelihoods { get; set; }

        public static ImputedGenotypes Empty()
        {
            return new ImputedGenotypes
            {
                Genotypes = new List<ImputedGenotype>(),
                SumOfLikelihoods = 0
            };
        }
    }
}
