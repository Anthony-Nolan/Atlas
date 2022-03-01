using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.ExternalInterface.Models;

namespace Atlas.MatchPrediction.Services.MatchProbability
{
    internal struct TruncatedGenotypeSet
    {
        public Dictionary<PhenotypeInfo<string>,decimal> GenotypeLikelihoods { get; set; }
        public ISet<PhenotypeInfo<HlaAtKnownTypingCategory>> Genotypes { get; set; }
    }
    
    /// <summary>
    /// Atlas cannot run suitably quickly when the expanded number of available genotypes for the patient/donor are too high
    /// (stemming from a combination of ambiguous typing, and very large haplotype frequency sets)
    /// This service encapsulates the logic of removing statistically insignificant genotypes from the expanded set before match calculation occurs.
    ///
    /// When the number of genotypes is sufficiently large, it has been observed that many of the frequencies of expanded genotypes are significantly
    /// less likely than others (several orders of magnitude), and so this truncation works on the assumption that taking only the most common genotypes,
    /// truncated to a number the algorithm can run in a reasonable timeframe, will not significantly affect the final probability outputs.  
    /// </summary>
    internal static class ExpandedGenotypeTruncater
    {
        /// <summary>
        /// The simplest truncation approach is to determine an "acceptable" number of genotypes to expand to for each of patient/donor.
        /// The higher this number, the higher the accuracy of the prediction results, but the slower the algorithm will be.
        ///
        /// There are two oversights to this approach that could be improved:
        /// * Patient/Donor may not need to be treated independently
        ///     e.g. if the patient has only a small number of possibilities, the donor can afford to include more possible genotypes
        /// * Relative likelihoods are ignored. e.g. we may fare better asking for a fixed number of orders of magnitude of genotype likelihoods, rather than a fixed number.
        ///     This could allow some searches to run even faster, in the case of a relatively small number of genotypes being significantly more likely
        ///     It would also allow us to have more faith in the accuracy of the results - as we'd confirm that we're only ever discarding statistically insignificant values
        ///     However this would come at a cost of not being able to guarantee the necessary performance of the match prediction algorithm. 
        /// </summary>
        private const int MaximumExpandedGenotypesPerInput = 1000;

        public static TruncatedGenotypeSet TruncateGenotypes(
            Dictionary<PhenotypeInfo<string>, decimal> likelihoods,
            ISet<PhenotypeInfo<HlaAtKnownTypingCategory>> genotypes)
        {
            var truncatedLikelihoods = likelihoods.OrderByDescending(g => g.Value).Take(MaximumExpandedGenotypesPerInput).ToDictionary();
            var truncatedGenotypes = genotypes.Where(p => truncatedLikelihoods.ContainsKey(p.ToHlaNames())).ToHashSet();

            return new TruncatedGenotypeSet { GenotypeLikelihoods = truncatedLikelihoods, Genotypes = truncatedGenotypes };
        }
    }
}