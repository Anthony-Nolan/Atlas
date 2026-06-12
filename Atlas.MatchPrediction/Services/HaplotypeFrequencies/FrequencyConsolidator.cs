using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Utils.Extensions;
using HaplotypeHla = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.LociInfo<string>;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies;

internal interface IFrequencyConsolidator
{
    /// <summary>
    /// Calculates all frequency values for allowed sets of missing loci: C, DQB1, C+DQB1
    /// </summary>
    /// <param name="frequencies">Source frequency set. Keys in the result are interned by <see cref="FrequencySetCacheEntry.Interner"/>.</param>
    /// <returns>
    /// A dictionary:
    ///     Key = Haplotypes (with the excluded locus/loci set to null)
    ///     Value = consolidated frequency (sum of all frequencies that match at non-excluded loci)
    /// </returns>
    FrozenDictionary<HaplotypeKey, decimal> PreConsolidateFrequenciesForCommonMissingLoci(FrequencySetCacheEntry frequencies);

    /// <summary>
    /// Calculates the consolidated frequency for a single haplotype, excluding certain loci.
    ///
    /// The results of <see cref="PreConsolidateFrequenciesForCommonMissingLoci"/> always include the value this method provides.
    /// It exists as a quicker way to calculate a single haplotype, while the full pre-consolidated list is calculated in the background.
    /// </summary>
    /// <param name="frequencies">Source frequency set.</param>
    /// <param name="hla">Hla to consolidate. Excluded loci need not be null, as they are provided independently.</param>
    /// <param name="excludedLoci">Loci to ignore when consolidating frequency values.</param>
    decimal ConsolidateFrequenciesForHaplotype(
        FrequencySetCacheEntry frequencies,
        HaplotypeHla hla,
        ISet<Locus> excludedLoci);
}

internal class FrequencyConsolidator : IFrequencyConsolidator
{
    /// <inheritdoc />
    public FrozenDictionary<HaplotypeKey, decimal> PreConsolidateFrequenciesForCommonMissingLoci(FrequencySetCacheEntry frequencies)
    {
        IEnumerable<(HaplotypeKey Key, decimal Frequency)> FrequenciesExcluding(params Locus[] loci)
        {
            return frequencies.SetFrequencies
                .GroupBy(hf => hf.Key.RemoveLoci(loci))
                .Select(group => (
                    group.Key,
                    Frequency: group.Select(f => f.Value.Frequency).SumDecimals()
                ));
        }

        // The result is keyed by frequencies.Interner (RemoveLoci only zeroes excluded loci on existing interned keys),
        // and is stored back onto that same entry - so a shared interner can never outlive a single cache lifetime.
        return FrequenciesExcluding(Locus.C)
            .Concat(FrequenciesExcluding(Locus.Dqb1))
            .Concat(FrequenciesExcluding(Locus.Dqb1, Locus.C))
            .ToFrozenDictionary(x => x.Key, x => x.Frequency);
    }

    /// <inheritdoc />
    public decimal ConsolidateFrequenciesForHaplotype(
        FrequencySetCacheEntry frequencies,
        HaplotypeHla hla,
        ISet<Locus> excludedLoci)
    {
        var excluded = excludedLoci.ToArray();

        // Mirror the grouping key used by PreConsolidateFrequenciesForCommonMissingLoci, so a single direct
        // calculation always agrees with the value the background-warmed collection will eventually hold.
        var keyToSeek = frequencies.Interner
            .ConvertWherePossible(hla.A, hla.B, hla.C, hla.Dqb1, hla.Drb1)
            .RemoveLoci(excluded);

        return frequencies.SetFrequencies
            .Where(hf => hf.Key.RemoveLoci(excluded) == keyToSeek)
            .Select(hf => hf.Value.Frequency)
            .DefaultIfEmpty(0m)
            .SumDecimals();
    }
}