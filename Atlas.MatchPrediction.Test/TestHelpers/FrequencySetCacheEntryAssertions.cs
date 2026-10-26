using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using AwesomeAssertions;

namespace Atlas.MatchPrediction.Test.TestHelpers;

public static class FrequencySetCacheEntryAssertions
{
    /// <summary>
    /// The same keys and values, in the same enumeration order, and the frequencies with the same bits; the same interner
    /// ids; the same projected pool, whose order is the tie-break of truncation; and the same allele index.
    /// </summary>
    public static void ShouldBeTheSameEntryAs(this FrequencySetCacheEntry actual, FrequencySetCacheEntry expected)
    {
        actual.SetFrequencies.Select(Comparable).Should().Equal(expected.SetFrequencies.Select(Comparable));

        foreach (var locus in new[] { Locus.A, Locus.B, Locus.C, Locus.Dqb1, Locus.Drb1 })
        {
            NamesById(actual.Interner.ForLocus(locus)).Should().Equal(NamesById(expected.Interner.ForLocus(locus)));
        }

        actual.ProjectedPool.Should().BeEquivalentTo(expected.ProjectedPool, options => options.WithStrictOrdering());
        actual.AlleleIndex.Should().BeEquivalentTo(
            expected.AlleleIndex,
            options => options.ComparingByMembers<LociInfo<int[][]>>().WithStrictOrdering());
    }

    private static (HaplotypeKey Key, string FrequencyBits, HaplotypeTypingCategory Category) Comparable(
        KeyValuePair<HaplotypeKey, HaplotypeFrequencyValue> frequency) =>
        (frequency.Key, string.Join(",", decimal.GetBits(frequency.Value.Frequency)), frequency.Value.TypingCategory);

    private static IEnumerable<string> NamesById(AlleleInterner alleles) => Enumerable.Range(1, alleles.IdCount - 1).Select(alleles.GetName);
}
