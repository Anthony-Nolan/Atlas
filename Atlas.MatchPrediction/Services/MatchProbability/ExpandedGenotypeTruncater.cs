using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.CompressedPhenotypeExpansion;

// The truncation key. Group names at the resolution the HF set stored, typing category erased - which is why two
// genotypes differing only in category share a slot here, and why the cap counts NAME forms, not genotypes.
using HfSetGenotypeNames = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.PhenotypeInfo<string>;

namespace Atlas.MatchPrediction.Services.MatchProbability
{
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
        /// This "acceptable" number is configurable per environment via <see cref="Atlas.MatchPrediction.ExternalInterface.Settings.GenotypeImputationSettings.MaximumExpandedGenotypesPerInput"/>
        /// and must be kept identical across every host that runs this code (see that settings class for details).
        /// Note: increasing it beyond the historical default of 2000 trades performance/memory for accuracy - validate any
        /// change via the Match Prediction validation (Gherkin) suite.
        ///
        /// There are two oversights to this approach that could be improved:
        /// * Patient/Donor may not need to be treated independently
        ///     e.g. if the patient has only a small number of possibilities, the donor can afford to include more possible genotypes
        /// * Relative likelihoods are ignored. e.g. we may fare better asking for a fixed number of orders of magnitude of genotype likelihoods, rather than a fixed number.
        ///     This could allow some searches to run even faster, in the case of a relatively small number of genotypes being significantly more likely
        ///     It would also allow us to have more faith in the accuracy of the results - as we'd confirm that we're only ever discarding statistically insignificant values
        ///     However this would come at a cost of not being able to guarantee the necessary performance of the match prediction algorithm.
        /// </summary>
        /// <param name="likelihoods">The likelihood of each distinct genotype, keyed by its name identity.</param>
        /// <param name="expanded">
        /// The expansion, which carries each genotype as a pair of pool indices plus its <c>GenotypeNameKey</c>, index
        /// for index, so membership of the kept key set is tested without deriving a name form. Both the name form and
        /// the genotype are built <b>here</b>, for the survivors only - a capped donor keeps 2,000 of up to 1.65M.
        /// </param>
        /// <param name="maximumExpandedGenotypesPerInput">The cap - at most this many genotypes are kept.</param>
        public static ImputedGenotypes TruncateGenotypes(
            Dictionary<GenotypeNameKey, decimal> likelihoods,
            ExpandedGenotypes expanded,
            int maximumExpandedGenotypesPerInput)
        {
            var truncatedLikelihoods = MostLikelyFirst(likelihoods, maximumExpandedGenotypesPerInput);

            // The name forms are built HERE, one per surviving key, because this is the first point at which
            // which-keys-survive is known - at most the cap, rather than one per pair the expansion kept.
            //
            // Each is stored against its key rather than used AS a key. Keyed on the eight bytes of a GenotypeNameKey,
            // one lookup yields both the name form and the likelihood, so a consumer needs neither to rebuild a name
            // form nor to probe a phenotype-keyed dictionary with it.
            //
            // The key maps to a dense INDEX into two parallel lists, rather than to a (name form, likelihood) tuple,
            // and that matters: a 24-byte dictionary value puts the entries array over the 85 KB large-object threshold
            // at exactly the 2,000-genotype cap, which costs a Gen2 collection per capped donor. Two lists of the same
            // length stay below it.
            //
            // Enumerated in MostLikelyFirst's order and summed in that order, which is the property that method exists
            // to guarantee: descending likelihood, and therefore a fixed decimal addition order. The sum is over KEYS,
            // not over genotypes - two genotypes differing only in typing category share one key and must not be
            // counted twice.
            var indexByKey = new Dictionary<GenotypeNameKey, int>(truncatedLikelihoods.Count);
            var namesByIndex = new List<HfSetGenotypeNames>(truncatedLikelihoods.Count);
            var likelihoodByIndex = new List<decimal>(truncatedLikelihoods.Count);
            var sumOfLikelihoods = 0m;

            foreach (var (nameKey, likelihood) in truncatedLikelihoods)
            {
                indexByKey[nameKey] = namesByIndex.Count;
                namesByIndex.Add(expanded.MaterialiseNames(nameKey));
                likelihoodByIndex.Add(likelihood);
                sumOfLikelihoods += likelihood;
            }

            // An indexed loop over the two parallel lists, rather than LINQ over the genotypes: this runs once per
            // PRE-truncation genotype, so a lambda and an enumerator here are paid up to 1.65M times per donor.
            //
            // Materialise() is inside the branch, so the seven objects a PhenotypeInfo costs are spent on the genotypes
            // that survive - at most the cap, plus any that share a surviving name key - rather than on all 1.65M.
            //
            // A List, not a HashSet. Distinct (i, j) always give a distinct genotype - PhenotypeInfo equality is
            // positional, the pool the indices address comes out of a HashSet so its members are distinct, and
            // HlaAtKnownTypingCategory's equality includes the typing category - so a set here would have nothing to
            // de-duplicate and would hash a twelve-object phenotype per survivor to find that out. Insertion order is
            // what a HashSet enumerated in anyway, so every downstream sequence - GenotypeConverter's output, the
            // cartesian product, its decimal sums - is unaffected.
            var survivors = new List<ImputedGenotype>(truncatedLikelihoods.Count);
            for (var i = 0; i < expanded.GenotypeCount; i++)
            {
                if (indexByKey.TryGetValue(expanded.GenotypeNameKeys[i], out var index))
                {
                    survivors.Add(new ImputedGenotype(
                        expanded.Materialise(i), namesByIndex[index], likelihoodByIndex[index]));
                }
            }

            return new ImputedGenotypes
            {
                Genotypes = survivors,
                SumOfLikelihoods = sumOfLikelihoods
            };
        }

        /// <summary>
        /// The most likely <paramref name="maximum"/> genotypes, in descending order of likelihood.
        ///
        /// <para>
        /// Above the cap this is O(N log maximum) with a bounded queue, rather than sorting all N entries - which can
        /// reach 1.65M for 2,000 kept - and buffering every one of them.
        /// </para>
        ///
        /// <para>
        /// <b>The selection rule is <c>(likelihood descending, insertion order ascending)</c>, ties included.</b>
        /// Insertion order is pairing order, which is survivor order, which is the order
        /// <c>FrequencySetCacheEntry.ProjectPool</c> exists to preserve. Which genotypes a capped donor keeps when
        /// likelihoods tie is a clinical output, so the tie-break is explicit here rather than left to a heap's
        /// arbitrary eviction. <c>ExpandedGenotypeTruncaterTests</c> pins it.
        /// </para>
        /// </summary>
        private static Dictionary<GenotypeNameKey, decimal> MostLikelyFirst(
            Dictionary<GenotypeNameKey, decimal> likelihoods,
            int maximum)
        {
            if (likelihoods.Count <= maximum)
            {
                // Nothing is discarded, so there is nothing to select - and N is at most the cap here, so a plain sort
                // is already cheap. It also fixes the enumeration order of the result, which the bounded path below
                // reproduces.
                return likelihoods.OrderByDescending(g => g.Value).ToDictionary();
            }

            // Priority is (likelihood, -insertionIndex) and PriorityQueue dequeues the MINIMUM, so the head is always
            // the entry the cap should drop first: least likely, and among equals the one inserted latest. That is what
            // lets EnqueueDequeue carry the whole tie-break rule with no comparison of its own - a candidate worse than
            // the head becomes the new minimum and leaves again immediately, which is precisely "of two tied keys, the
            // earlier one survives".
            var mostLikely =
                new PriorityQueue<GenotypeNameKey, (decimal Likelihood, int NegatedInsertionIndex)>(maximum);
            var insertionIndex = 0;

            foreach (var (genotype, likelihood) in likelihoods)
            {
                var priority = (likelihood, -insertionIndex);

                if (mostLikely.Count < maximum)
                {
                    mostLikely.Enqueue(genotype, priority);
                }
                else
                {
                    mostLikely.EnqueueDequeue(genotype, priority);
                }

                insertionIndex++;
            }

            // Descending by the same priority tuple gives (likelihood descending, insertion order ascending), so the
            // kept dictionary enumerates - and therefore SumDecimals adds - in that order.
            return mostLikely.UnorderedItems
                .OrderByDescending(item => item.Priority)
                .ToDictionary(item => item.Element, item => item.Priority.Likelihood);
        }
    }
}
