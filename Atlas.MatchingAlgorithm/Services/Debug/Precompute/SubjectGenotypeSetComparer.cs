#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Models;

namespace Atlas.MatchingAlgorithm.Services.Debug.Precompute;

/// <summary>
/// Compares a genotype set computed live with a stored set, for the parity check of the genotype set debug endpoint.
/// </summary>
/// <remarks>
/// <para>
/// <b>The genotypes are a multiset.</b> Their order does not matter. A genotype is identified by its two resolutions,
/// compared by value. The two positions of a locus are compared as they are: the engine is deterministic, so a swap is a
/// finding. One genotype can occur more than once in a set (two genotypes that differ only in typing category have the
/// same names), so the likelihoods of each genotype are compared in sorted order.
/// </para>
///
/// <para>
/// <b>Decimals are compared by value</b>, with no rounding and no tolerance. So <c>1.10</c> and <c>1.1</c> are equal:
/// they give the same probabilities.
/// </para>
///
/// <para>
/// <b>The frequency sets are compared first.</b> For a donor, the stored row can have another frequency set than the
/// live set: the row is out of date, or the request names another set. The other differences then come from the two
/// sets, not from the stored data, so the difference of the sets comes first and tells why the others are there.
/// </para>
/// </remarks>
internal static class SubjectGenotypeSetComparer
{
    /// <summary>
    /// Gives the differences in a fixed order: first those of the whole set (frequency set, unrepresented, genotype count,
    /// sum of likelihoods), then those of each genotype, ordered by the text of its two resolutions.
    /// </summary>
    /// <param name="live">The set computed live.</param>
    /// <param name="liveFrequencySetId">The frequency set that <paramref name="live"/> was computed with.</param>
    /// <param name="stored">The decoded set of the stored row.</param>
    /// <param name="storedFrequencySetId">The frequency set of the stored row.</param>
    /// <param name="maxDifferences">The most differences to give. All the differences are counted.</param>
    public static GenotypeSetComparison Compare(
        SubjectGenotypeSet live,
        int liveFrequencySetId,
        SubjectGenotypeSet stored,
        int storedFrequencySetId,
        int maxDifferences)
    {
        var differences = new List<GenotypeSetDifference>();
        var differenceCount = 0;

        void Add(GenotypeSetDifferenceKind kind, Genotype? genotype, string? liveValue, string? storedValue)
        {
            differenceCount++;
            if (differences.Count < maxDifferences)
            {
                differences.Add(new GenotypeSetDifference(
                    kind,
                    genotype?.StringMatchableResolution,
                    genotype?.HaplotypeResolution,
                    liveValue,
                    storedValue));
            }
        }

        var liveGenotypes = live.Genotypes ?? [];
        var storedGenotypes = stored.Genotypes ?? [];

        if (liveFrequencySetId != storedFrequencySetId)
        {
            Add(GenotypeSetDifferenceKind.HaplotypeFrequencySet, null, Format(liveFrequencySetId), Format(storedFrequencySetId));
        }

        if (live.IsUnrepresented != stored.IsUnrepresented)
        {
            Add(GenotypeSetDifferenceKind.IsUnrepresented, null, Format(live.IsUnrepresented), Format(stored.IsUnrepresented));
        }

        if (liveGenotypes.Count != storedGenotypes.Count)
        {
            Add(GenotypeSetDifferenceKind.GenotypeCount, null, Format(liveGenotypes.Count), Format(storedGenotypes.Count));
        }

        if (live.SumOfLikelihoods != stored.SumOfLikelihoods)
        {
            Add(GenotypeSetDifferenceKind.SumOfLikelihoods, null, Format(live.SumOfLikelihoods), Format(stored.SumOfLikelihoods));
        }

        var liveLikelihoods = LikelihoodsByGenotype(liveGenotypes);
        var storedLikelihoods = LikelihoodsByGenotype(storedGenotypes);

        foreach (var genotype in liveLikelihoods.Keys.Union(storedLikelihoods.Keys).OrderBy(SortText, StringComparer.Ordinal))
        {
            var liveValues = liveLikelihoods.GetValueOrDefault(genotype, []);
            var storedValues = storedLikelihoods.GetValueOrDefault(genotype, []);

            for (var index = 0; index < Math.Max(liveValues.Count, storedValues.Count); index++)
            {
                if (index >= storedValues.Count)
                {
                    Add(GenotypeSetDifferenceKind.GenotypeOnlyInLive, genotype, Format(liveValues[index]), null);
                }
                else if (index >= liveValues.Count)
                {
                    Add(GenotypeSetDifferenceKind.GenotypeOnlyInStored, genotype, null, Format(storedValues[index]));
                }
                else if (liveValues[index] != storedValues[index])
                {
                    Add(GenotypeSetDifferenceKind.GenotypeLikelihood, genotype, Format(liveValues[index]), Format(storedValues[index]));
                }
            }
        }

        return new GenotypeSetComparison(differenceCount, differences);
    }

    private static Dictionary<Genotype, List<decimal>> LikelihoodsByGenotype(IEnumerable<GenotypeAtDesiredResolutions> genotypes) =>
        genotypes
            .GroupBy(genotype => new Genotype(genotype.StringMatchableResolution, genotype.HaplotypeResolution))
            .ToDictionary(group => group.Key, group => group.Select(genotype => genotype.GenotypeLikelihood).Order().ToList());

    private static string SortText(Genotype genotype) =>
        $"{genotype.StringMatchableResolution?.PrettyPrint()}|{genotype.HaplotypeResolution?.PrettyPrint()}";

    private static string Format(bool value) => value ? "true" : "false";

    private static string Format(int value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>All the digits, with the scale the value has.</summary>
    private static string Format(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>A genotype without its likelihood. The two resolutions are compared by value.</summary>
    private readonly record struct Genotype(PhenotypeInfo<string>? StringMatchableResolution, PhenotypeInfo<string>? HaplotypeResolution);
}

/// <param name="DifferenceCount">The number of all the differences, also those that are not given.</param>
/// <param name="Differences">The first differences, in the fixed order of <see cref="SubjectGenotypeSetComparer.Compare"/>.</param>
internal sealed record GenotypeSetComparison(int DifferenceCount, IReadOnlyList<GenotypeSetDifference> Differences);
