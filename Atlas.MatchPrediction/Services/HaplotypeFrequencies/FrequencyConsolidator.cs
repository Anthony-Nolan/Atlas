using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.Data.Models;
using HaplotypeHla = Atlas.Common.GeneticData.PhenotypeInfo.LociInfo<string>;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies
{
    internal interface IFrequencyConsolidator
    {
        /// <summary>
        /// Calculates all frequency values for allowed sets of missing loci: C, DQB1, C+DQB1
        /// </summary>
        /// <param name="frequencies">Source frequency set.</param>
        /// <returns>
        /// A dictionary:
        ///     Key = Haplotypes (with the excluded locus/loci set to null)
        ///     Value = consolidated frequency (sum of all frequencies that match at non-excluded loci) 
        /// </returns>
        public ConcurrentDictionary<HaplotypeHla, decimal> PreConsolidateFrequencies(IDictionary<HaplotypeHla, HaplotypeFrequency> frequencies);

        /// <summary>
        /// Calculates the consolidated frequency at a given haplotype, excluding certain loci.
        ///
        /// The results of <see cref="PreConsolidateFrequencies"/> should always include all values that this method provides.
        /// It exists as a quicker way to calculate a single haplotype, while the full pre-consolidated list is calculated in the background.
        /// </summary>
        /// <param name="frequencies">Source frequency set.</param>
        /// <param name="hla">Hla to consolidate. Excluded loci need not be null, as they are provided independently.</param>
        /// <param name="excludedLoci">Loci to ignore when consolidating frequency values</param>
        /// <returns></returns>
        public decimal ConsolidateFrequenciesForHaplotype(
            IDictionary<HaplotypeHla, HaplotypeFrequency> frequencies,
            HaplotypeHla hla,
            ISet<Locus> excludedLoci);
    }

    internal class FrequencyConsolidator : IFrequencyConsolidator
    {
        /// <inheritdoc />
        public ConcurrentDictionary<HaplotypeHla, decimal> PreConsolidateFrequencies(IDictionary<HaplotypeHla, HaplotypeFrequency> frequencies)
        {
            return PreConsolidateFrequencies(frequencies.ToDictionary(f => f.Key, f => f.Value.Frequency));
        }

        private ConcurrentDictionary<HaplotypeHla, decimal> PreConsolidateFrequencies(IDictionary<HaplotypeHla, decimal> frequencies)
        {
            IEnumerable<KeyValuePair<HaplotypeHla, decimal>> FrequenciesExcluding(params Locus[] loci)
            {
                return frequencies
                    .GroupBy(hf => hf.Key.SetLoci(null, loci))
                    .Select(group => new KeyValuePair<HaplotypeHla, decimal>(
                        group.Key,
                        group.Select(f => f.Value).SumDecimals()
                    ));
            }

            var consolidated = FrequenciesExcluding(Locus.C)
                .Concat(FrequenciesExcluding(Locus.Dqb1))
                .Concat(FrequenciesExcluding(Locus.Dqb1, Locus.C))
                .ToDictionary();
            return new ConcurrentDictionary<HaplotypeHla, decimal>(consolidated);
        }

        /// <inheritdoc />
        public decimal ConsolidateFrequenciesForHaplotype(
            IDictionary<HaplotypeHla, HaplotypeFrequency> frequencies,
            HaplotypeHla hla,
            ISet<Locus> excludedLoci)
        {
            var allowedLoci = LocusSettings.MatchPredictionLoci.Except(excludedLoci).ToHashSet();

            return frequencies
                .Where(kvp => kvp.Key.EqualsAtLoci(hla, allowedLoci))
                .Select(kvp => kvp.Value.Frequency)
                .DefaultIfEmpty(0m)
                .SumDecimals();
        }
    }
}