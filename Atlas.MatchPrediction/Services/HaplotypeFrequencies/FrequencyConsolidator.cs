using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Utils.Extensions;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies;

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
    FrequencySetCacheEntry<decimal> PreConsolidateFrequenciesForCommonMissingLoci(FrequencySetCacheEntry<HaplotypeFrequencyValue> frequencies);
}

internal class FrequencyConsolidator : IFrequencyConsolidator
{
    /// <inheritdoc />
    public FrequencySetCacheEntry<decimal> PreConsolidateFrequenciesForCommonMissingLoci(FrequencySetCacheEntry<HaplotypeFrequencyValue> frequencies)
    {
        IEnumerable<(HaplotypeKey Key, decimal Frequency)> FrequenciesExcluding(params Locus[] loci)
        {
            return frequencies.Frequencies
                .GroupBy(hf => hf.Key.RemoveLoci(loci))
                .Select(group => (
                    group.Key,
                    Frequency: group.Select(f => f.Value.Frequency).SumDecimals()
                ));
        }
        // Need to add the stuff to an interner? Or create a new one? Problem - interner is now shared between two caches with possibly different TTLs? 

        var consolidated = FrequenciesExcluding(Locus.C)
            .Concat(FrequenciesExcluding(Locus.Dqb1))
            .Concat(FrequenciesExcluding(Locus.Dqb1, Locus.C))
            .ToDictionary(key => key.Key, value => value.Frequency);
        var cacheEntry = new FrequencySetCacheEntry<decimal>(consolidated.ToFrozenDictionary(), frequencies.Interner);
        return cacheEntry;
    }
}